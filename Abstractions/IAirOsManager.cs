using Ubiquiti.AirOs.Models;

namespace Ubiquiti.AirOs.Abstractions;

/// <summary>
/// Represents a manager for a specific airOS device firmware version.
/// Owns its device connection and releases it via <see cref="IAsyncDisposable"/>.
/// </summary>
/// <remarks>
/// Configuration is changed through the <c>Set*</c> methods, which only enqueue commands.
/// Nothing is written to the device until <see cref="ApplyChangesAsync"/> is called.
/// This mirrors the unit-of-work / <c>SaveChanges</c> pattern: mutate the in-memory queue,
/// then apply atomically.
/// </remarks>
public interface IAirOsManager : IAsyncDisposable
{
    /// <summary>Gets the number of commands queued for the next <see cref="ApplyChangesAsync"/> call.</summary>
    /// <remarks>Useful for UI to indicate unsaved changes (e.g. <c>PendingCommandsCount &gt; 0</c>).</remarks>
    int PendingCommandsCount { get; }

    // -------- Connection & Core Execution --------

    /// <summary>Establishes an SSH connection to the device.</summary>
    /// <param name="ipAddress">The IP address of the device. Must be a valid IPv4 address. Defaults to 192.168.1.20</param>
    /// <param name="username">The username for authentication. Defaults to <c>"ubnt"</c>.</param>
    /// <param name="password">The password for authentication. Defaults to <c>"ubnt"</c>.</param>
    /// <param name="port">The SSH port. Defaults to <c>22</c>.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous connection operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ipAddress"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ipAddress"/> is not a valid IPv4 address.</exception>
    /// <exception cref="Renci.SshNet.Common.SshException">Thrown when the SSH connection cannot be established.</exception>
    Task ConnectAsync(string ipAddress = "192.168.1.20", string username = "ubnt", string password = "ubnt", int port = 22, CancellationToken ct = default);

    /// <summary>Executes an arbitrary list of commands on the current connection without saving or restarting.</summary>
    /// <remarks>
    /// This is a low-level escape hatch. It does not interact with the pending command queue
    /// and does not create a backup. Use <see cref="ApplyChangesAsync"/> for transactional changes.
    /// </remarks>
    /// <param name="commands">The commands to execute. If empty, an empty successful result is returned.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A task returning the batch execution result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manager is not connected.</exception>
    Task<CommandResult> ExecuteBatchAsync(IReadOnlyList<string> commands, CancellationToken ct = default);

    /// <summary>
    /// Queues a raw shell command to be executed during the next apply.
    /// </summary>
    /// <param name="command">The shell command to queue. Must not be null or empty.</param>
    void AddCommand(string command);

    // -------- Queue Management & Apply --------

    /// <summary>Discards all pending commands that have not been applied yet.</summary>
    /// <remarks>Does not touch the device. Only clears the local queue.</remarks>
    void ClearCommands();

    /// <summary>Executes the pending commands in memory and creates a backup, without restarting the device.</summary>
    /// <remarks>
    /// This is the preparation phase. It modifies <c>/tmp/system.cfg</c> and saves a backup of
    /// the previous state, but does <b>not</b> apply the changes or persist them to flash.
    /// Use <see cref="ApplyChangesAsync"/> to make the changes active and permanent, or
    /// <see cref="DiscardAsync"/> to discard them.
    /// </remarks>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns><see langword="true"/> if all commands were executed successfully; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manager is not connected.</exception>
    Task<bool> ChangeAsync(CancellationToken ct = default);

    /// <summary>Persists the current configuration to flash and performs a soft restart.</summary>
    /// <remarks>
    /// This is the commit phase. It writes the modified <c>/tmp/system.cfg</c> to flash storage
    /// via <c>cfgmtd -w -p /etc</c> and performs a soft restart.
    /// If network parameters were changed, the SSH connection may drop during the restart.
    /// </remarks>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the configuration was saved and applied; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manager is not connected.</exception>
    Task<bool> ApplyChangesAsync(CancellationToken ct = default);

    /// <summary>Rolls back the configuration to the state before the last <see cref="ChangeAsync"/> call.</summary>
    /// <remarks>
    /// This restores the backup created during <see cref="ChangeAsync"/> and discards all pending commands.
    /// It does <b>not</b> write to flash. The changes take effect after the next <see cref="ApplyChangesAsync"/> call.
    /// </remarks>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the rollback was successful; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manager is not connected.</exception>
    Task<bool> DiscardAsync(CancellationToken ct = default);

    /// <summary>Performs a factory reset of the device and reboots it.</summary>
    /// <remarks>
    /// This wipes all configuration on the device, including credentials and network settings.
    /// The pending command queue is cleared. The SSH connection is expected to drop during reboot.
    /// </remarks>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous factory reset operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manager is not connected or the reset command fails.</exception>
    Task ResetToFactoryAsync(CancellationToken ct = default);

    /// <summary>Reboots the device without applying any pending changes.</summary>
    /// <remarks>
    /// Pending commands are <b>not</b> applied and are preserved in the queue.
    /// The SSH connection is expected to drop during reboot.
    /// </remarks>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous reboot operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manager is not connected.</exception>
    Task RebootAsync(CancellationToken ct = default);

    // -------- Configuration Setters --------

    /// <summary>Queues a change of the device username.</summary>
    /// <param name="username">The new username. Must not be <see langword="null"/> or empty.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="username"/> is <see langword="null"/>.</exception>
    IAirOsManager SetUsername(string username);

    /// <summary>Queues a change of the device password. The password is hashed internally before being sent.</summary>
    /// <param name="password">The new plain-text password. Must not be <see langword="null"/>.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="password"/> is <see langword="null"/>.</exception>
    IAirOsManager SetPassword(string password);

    /// <summary>Queues a change of the device hostname.</summary>
    /// <param name="hostname">The new hostname.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetHostname(string hostname);

    /// <summary>Queues a change of the static IP address.</summary>
    /// <param name="ip">The new IPv4 address. Must be a valid, non-loopback, non-link-local address.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ip"/> is not a valid IPv4 address.</exception>
    IAirOsManager SetStaticIp(string ip);

    /// <summary>Queues a change of the static subnet mask.</summary>
    /// <param name="netmask">The new IPv4 subnet mask (e.g. <c>"255.255.255.0"</c>).</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="netmask"/> is not a valid subnet mask.</exception>
    IAirOsManager SetStaticNetmask(string netmask);

    /// <summary>Queues a change of the static default gateway.</summary>
    /// <param name="gateway">The new gateway IPv4 address, or <see langword="null"/> / empty to disable the gateway.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="gateway"/> is non-empty and not a valid gateway address.</exception>
    IAirOsManager SetStaticGateway(string? gateway);

    /// <summary>Queues a change of the primary DNS server.</summary>
    /// <param name="dns">The primary DNS IPv4 address, or <see langword="null"/> / empty to disable it.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetStaticPrimaryDns(string? dns);

    /// <summary>Queues a change of the secondary DNS server.</summary>
    /// <param name="dns">The secondary DNS IPv4 address, or <see langword="null"/> / empty to disable it.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetStaticSecondaryDns(string? dns);

    /// <summary>Queues enabling or disabling DHCP.</summary>
    /// <param name="enabled"><see langword="true"/> to enable DHCP; <see langword="false"/> to disable it.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetDhcp(bool enabled);

    /// <summary>Queues a change of the fallback IP address (used when DHCP fails).</summary>
    /// <param name="ip">The fallback IPv4 address.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ip"/> is not a valid IPv4 address.</exception>
    IAirOsManager SetFallbackIp(string ip);

    /// <summary>Queues a change of the fallback subnet mask (used when DHCP fails).</summary>
    /// <param name="netmask">The fallback subnet mask.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="netmask"/> is not a valid subnet mask.</exception>
    IAirOsManager SetFallbackNetmask(string netmask);

    /// <summary>Queues a change of the wireless SSID for the specified operation mode.</summary>
    /// <remarks>
    /// If you do not know the current mode, call <see cref="GetWirelessModeAsync"/> first.
    /// Do not rely on a default — an incorrect mode produces an invalid configuration
    /// and may leave the device unreachable or unable to associate.
    /// </remarks>
    /// <param name="ssid">The new SSID.</param>
    /// <param name="mode">The current wireless operation mode (Access Point or Station).</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetSsid(string ssid, WirelessMode mode);

    /// <summary>Queues a change of the wireless operation mode.</summary>
    /// <param name="mode">The new wireless operation mode.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mode"/> is not a supported value.</exception>
    IAirOsManager SetWirelessMode(WirelessMode mode);

    /// <summary>Queues a change of the channel width.</summary>
    /// <param name="channelWidth">The channel width in MHz (e.g. <c>20</c>, <c>40</c>).</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetChannel(int channelWidth);

    /// <summary>Queues a change of the transmit power.</summary>
    /// <param name="power">The transmit power in dBm.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetTxPower(int power);

    /// <summary>Queues enabling or disabling the watchdog timer.</summary>
    /// <param name="enabled"><see langword="true"/> to enable the watchdog; <see langword="false"/> to disable it.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetWatchdogEnabled(bool enabled);

    /// <summary>Queues a change of the watchdog host address.</summary>
    /// <param name="host">The watchdog host IPv4 address. Must be a valid IPv4 address.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="host"/> is not a valid IPv4 address.</exception>
    IAirOsManager SetWatchdogHost(string host);

    /// <summary>Queues a change of the watchdog check period.</summary>
    /// <param name="period">The check period in seconds.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetWatchdogPeriod(int period);

    /// <summary>Queues a change of the watchdog initial delay.</summary>
    /// <param name="delay">The initial delay in seconds.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetWatchdogDelay(int delay);

    /// <summary>Queues a change of the watchdog retry count.</summary>
    /// <param name="retry">The number of retries.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetWatchdogRetry(int retry);

    /// <summary>
    /// Queues enabling or disabling WPA2 security for the specified operation mode.
    /// </summary>
    /// <param name="enabled"><see langword="true"/> to enable WPA2; <see langword="false"/> for an open network.</param>
    /// <param name="mode">The current wireless operation mode (Access Point or Station).</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager SetWpaSecurityEnabled(bool enabled, WirelessMode mode);

    /// <summary>
    /// Queues a change of the WPA2 pre-shared key for the specified operation mode.
    /// </summary>
    /// <param name="password">The WPA2 pre-shared key. Must not be <see langword="null"/> or empty.</param>
    /// <param name="mode">The current wireless operation mode (Access Point or Station).</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="password"/> is <see langword="null"/> or empty.</exception>
    IAirOsManager SetWpaPassword(string password, WirelessMode mode);

    /// <summary>Queues a change of the country code.</summary>
    /// <param name="countryCode">The numeric country code. Must be greater than zero.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="countryCode"/> is less than or equal to zero.</exception>
    IAirOsManager SetCountry(int countryCode);

    /// <summary>Queues acceptance of the end-user license agreement (EULA).</summary>
    /// <param name="acceptedDate">The acceptance date. Defaults to the current UTC date when <see langword="null"/>.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    IAirOsManager AcceptEula(DateTime? acceptedDate = null);

    // -------- Configuration Getters --------

    /// <summary>Gets the device hostname.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The hostname, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetHostnameAsync(CancellationToken ct = default);

    /// <summary>Gets the static IP address from the configuration.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The IP address, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetStaticIpAsync(CancellationToken ct = default);

    /// <summary>Gets the static subnet mask from the configuration.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The subnet mask, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetStaticNetmaskAsync(CancellationToken ct = default);

    /// <summary>Gets the static gateway from the configuration.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The gateway address, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetStaticGatewayAsync(CancellationToken ct = default);

    /// <summary>Gets the primary DNS from the configuration.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The primary DNS address, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetStaticPrimaryDnsAsync(CancellationToken ct = default);

    /// <summary>Gets the secondary DNS from the configuration.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The secondary DNS address, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetStaticSecondaryDnsAsync(CancellationToken ct = default);

    /// <summary>Gets the fallback IP address from the configuration.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The fallback IP address, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetFallbackIpAsync(CancellationToken ct = default);

    /// <summary>Gets the fallback subnet mask from the configuration.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The fallback subnet mask, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetFallbackNetmaskAsync(CancellationToken ct = default);

    /// <summary>Gets whether DHCP is enabled.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns><see langword="true"/> if enabled; <see langword="false"/> if disabled; <see langword="null"/> if unknown.</returns>
    Task<bool?> GetDhcpAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the DHCP configuration (IP, netmask, gateway, DNS) in a single request.
    /// </summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="DhcpInfo"/> object, or <see langword="null"/> if DHCP is not active or unavailable.</returns>
    Task<DhcpInfo?> GetDhcpInfoAsync(CancellationToken ct = default);

    /// <summary>Gets the wireless SSID.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The current SSID, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetSsidAsync(CancellationToken ct = default);

    /// <summary>Gets the current wireless operation mode from the device.</summary>
    /// <remarks>
    /// Reads <c>radio.1.mode</c> from <c>/tmp/system.cfg</c>. Returns <see langword="null"/> when the value
    /// is missing or could not be read (for example, when the device is unreachable). Callers that require
    /// a definite mode should treat <see langword="null"/> as an error rather than falling back silently.
    /// </remarks>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The current mode, or <see langword="null"/> if it could not be determined.</returns>
    Task<WirelessMode?> GetWirelessModeAsync(CancellationToken ct = default);

    /// <summary>Gets the channel width.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The channel width in MHz, or <see langword="null"/> if unavailable.</returns>
    Task<int?> GetChannelWidthAsync(CancellationToken ct = default);

    /// <summary>Gets the transmit power.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The transmit power in dBm, or <see langword="null"/> if unavailable.</returns>
    Task<int?> GetTxPowerAsync(CancellationToken ct = default);

    /// <summary>Gets whether WPA security is currently enabled.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns><see langword="true"/> if WPA is enabled; otherwise <see langword="false"/>.</returns>
    Task<bool> GetWpaSecurityEnabledAsync(CancellationToken ct = default);

    /// <summary>Gets the current WPA pre-shared key.</summary>
    /// <remarks>
    /// Reads the PSK from the appropriate configuration section based on the current wireless mode.
    /// </remarks>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The WPA PSK, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetWpaPasswordAsync(CancellationToken ct = default);

    /// <summary>Gets the numeric country code.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The country code, or <see langword="null"/> if unavailable.</returns>
    Task<int?> GetCountryCodeAsync(CancellationToken ct = default);

    /// <summary>Gets the country name.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The country name, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetCountryNameAsync(CancellationToken ct = default);

    /// <summary>Gets the country code and name together.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CountryInfo"/>, or <see langword="null"/> if unavailable.</returns>
    Task<CountryInfo?> GetCountryInfoAsync(CancellationToken ct = default);

    /// <summary>Gets whether the EULA has been accepted.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns><see langword="true"/> if accepted; <see langword="false"/> if not; <see langword="null"/> if unknown.</returns>
    Task<bool?> GetEulaAcceptedAsync(CancellationToken ct = default);

    /// <summary>Gets the EULA acceptance date.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The acceptance date, or <see langword="null"/> if unavailable.</returns>
    Task<DateTime?> GetEulaAcceptedDateAsync(CancellationToken ct = default);

    /// <summary>Gets whether the watchdog is enabled.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns><see langword="true"/> if enabled; <see langword="false"/> if disabled; <see langword="null"/> if unknown.</returns>
    Task<bool?> GetWatchdogEnabledAsync(CancellationToken ct = default);

    /// <summary>Gets the watchdog host.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The watchdog host address, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetWatchdogHostAsync(CancellationToken ct = default);

    /// <summary>Gets the watchdog period.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The period in seconds, or <see langword="null"/> if unavailable.</returns>
    Task<int?> GetWatchdogPeriodAsync(CancellationToken ct = default);

    /// <summary>Gets the watchdog delay.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The delay in seconds, or <see langword="null"/> if unavailable.</returns>
    Task<int?> GetWatchdogDelayAsync(CancellationToken ct = default);

    /// <summary>Gets the watchdog retry count.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The retry count, or <see langword="null"/> if unavailable.</returns>
    Task<int?> GetWatchdogRetryAsync(CancellationToken ct = default);

    /// <summary>Retrieves the system log from the device.</summary>
    /// <param name="lines">The number of lines to retrieve. Defaults to 100.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The log text, or <see langword="null"/> if it could not be retrieved.</returns>
    Task<string?> GetSystemLogAsync(int lines = 100, CancellationToken ct = default);

    /// <summary>Gets the full device configuration as a string.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The full configuration text, or <see langword="null"/> if unavailable.</returns>
    Task<string?> GetFullConfigAsync(CancellationToken ct = default);

    /// <summary>Gets the current firmware version installed on the device.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="FirmwareVersionInfo"/>, or <see langword="null"/> if unavailable.</returns>
    Task<FirmwareVersionInfo?> GetFirmwareVersionAsync(CancellationToken ct = default);

    /// <summary>Uploads and installs firmware from a byte array.</summary>
    /// <param name="firmwareContent">The firmware binary content. Must not be <see langword="null"/> or empty.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the installation completed successfully; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="firmwareContent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="firmwareContent"/> is empty.</exception>
    Task<bool> UpgradeFirmwareAsync(byte[] firmwareContent, CancellationToken ct = default);

    /// <summary>Uploads and installs firmware from a stream.</summary>
    /// <param name="firmwareStream">The stream containing the firmware binary content. Must not be <see langword="null"/>.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the installation completed successfully; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="firmwareStream"/> is <see langword="null"/>.</exception>
    Task<bool> UpgradeFirmwareAsync(Stream firmwareStream, CancellationToken ct = default);

    // -------- Monitoring --------

    /// <summary>Gets the current radio and system status of the device.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The parsed radio status, or <see langword="null"/> if it could not be retrieved.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the underlying <c>mca-status</c> command fails.</exception>
    Task<AirOsRadioStatus?> GetRadioStatusAsync(CancellationToken ct = default);

    /// <summary>Gets the list of wireless stations currently connected to the device.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of connected stations. Empty when none are connected or parsing fails.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the underlying <c>wstalist</c> command fails.</exception>
    Task<IReadOnlyList<AirOsStation>> GetStationsAsync(CancellationToken ct = default);

    /// <summary>Downloads the current device configuration file.</summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The raw configuration file content, or <see langword="null"/> if unavailable.</returns>
    Task<string?> DownloadConfigAsync(CancellationToken ct = default);

    /// <summary>Uploads a complete configuration file to the device and reboots it to apply the changes.</summary>
    /// <remarks>
    /// This bypasses the pending command queue and the transactional apply flow.
    /// The uploaded content replaces <c>/tmp/system.cfg</c> entirely. Use with care.
    /// </remarks>
    /// <param name="configContent">The full configuration file content. Must not be <see langword="null"/> or whitespace.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the upload and reboot were initiated successfully; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="configContent"/> is <see langword="null"/> or whitespace.</exception>
    Task<bool> UploadConfigAsync(string configContent, CancellationToken ct = default);
}