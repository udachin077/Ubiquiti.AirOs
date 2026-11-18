using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Renci.SshNet;
using Ubiquiti.AirOs.Abstractions;
using Ubiquiti.AirOs.Models;

namespace Ubiquiti.AirOs.Managers;

/// <summary>
/// SSH interaction manager for devices running airOS 8.
/// </summary>
/// <remarks>
/// This is currently a placeholder implementation. The airOS 8 command syntax differs from airOS 6
/// and must be implemented separately. Every member throws <see cref="NotImplementedException"/>
/// to make the limitation explicit at the call site.
/// </remarks>
public sealed class AirOs8Manager : IAirOsManager
{
    private const string NotSupportedMessage =
        "airOS 8 management is not implemented yet. Use airOS 6 devices or implement the airOS 8 command syntax.";

    private readonly ILogger<AirOs8Manager> _logger;
    private readonly IIpValidator _ipValidator;
    private readonly IUbiquitiPasswordHasher _passwordHasher;
    private SshClient? _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AirOs8Manager"/> class.
    /// </summary>
    /// <param name="ipValidator">The IP address validator. Must not be <see langword="null"/>.</param>
    /// <param name="passwordHasher">The password hasher. Must not be <see langword="null"/>.</param>
    /// <param name="logger">The optional logger instance. Defaults to a no-op logger.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="ipValidator"/> or <paramref name="passwordHasher"/> is <see langword="null"/>.
    /// </exception>
    public AirOs8Manager(IIpValidator ipValidator, IUbiquitiPasswordHasher passwordHasher, ILogger<AirOs8Manager>? logger = null)
    {
        _ipValidator = ipValidator ?? throw new ArgumentNullException(nameof(ipValidator));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _logger = logger ?? NullLogger<AirOs8Manager>.Instance;
    }

    /// <inheritdoc />
    public int PendingCommandsCount => throw new NotImplementedException(NotSupportedMessage);

    // -------- Connection & Core Execution --------

    /// <inheritdoc />
    public Task ConnectAsync(string ipAddress, string username = "ubnt", string password = "ubnt", int port = 22, CancellationToken ct = default)
        => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<CommandResult> ExecuteBatchAsync(IReadOnlyList<string> commands, CancellationToken ct = default)
        => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public void AddCommand(string command) => throw new NotImplementedException(NotSupportedMessage);

    // -------- Queue Management & Apply --------

    /// <inheritdoc />
    public void ClearCommands() => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<bool> ChangeAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<bool> ApplyChangesAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<bool> DiscardAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task ResetToFactoryAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task RebootAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    // -------- Configuration Setters --------

    /// <inheritdoc />
    public IAirOsManager SetUsername(string username) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetPassword(string password) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetHostname(string hostname) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetStaticIp(string ip) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetStaticNetmask(string netmask) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetStaticGateway(string? gateway) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetStaticPrimaryDns(string? dns) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetStaticSecondaryDns(string? dns) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetDhcp(bool enabled) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetFallbackIp(string ip) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetFallbackNetmask(string netmask) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetWirelessMode(WirelessMode mode) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetSsid(string ssid, WirelessMode mode) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetChannel(int channelWidth) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetTxPower(int power) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetWpaSecurityEnabled(bool enabled, WirelessMode mode) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetWpaPassword(string password, WirelessMode mode) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetWatchdogEnabled(bool enabled) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetWatchdogHost(string host) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetWatchdogPeriod(int period) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetWatchdogDelay(int delay) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetWatchdogRetry(int retry) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager SetCountry(int countryCode) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public IAirOsManager AcceptEula(DateTime? acceptedDate = null) => throw new NotImplementedException(NotSupportedMessage);

    // -------- Configuration Getters --------

    /// <inheritdoc />
    public Task<string?> GetHostnameAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetStaticIpAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetStaticNetmaskAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetStaticGatewayAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetStaticPrimaryDnsAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetStaticSecondaryDnsAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetFallbackIpAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetFallbackNetmaskAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<bool?> GetDhcpAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<DhcpInfo?> GetDhcpInfoAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetSsidAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<WirelessMode?> GetWirelessModeAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<int?> GetChannelWidthAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<int?> GetTxPowerAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<bool> GetWpaSecurityEnabledAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetWpaPasswordAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<int?> GetCountryCodeAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetCountryNameAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<CountryInfo?> GetCountryInfoAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<bool?> GetEulaAcceptedAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<DateTime?> GetEulaAcceptedDateAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<bool?> GetWatchdogEnabledAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetWatchdogHostAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<int?> GetWatchdogPeriodAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<int?> GetWatchdogDelayAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<int?> GetWatchdogRetryAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetSystemLogAsync(int lines = 100, CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> GetFullConfigAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<FirmwareVersionInfo?> GetFirmwareVersionAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<bool> UpgradeFirmwareAsync(byte[] firmwareContent, CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<bool> UpgradeFirmwareAsync(Stream firmwareStream, CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<AirOsRadioStatus?> GetRadioStatusAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<IReadOnlyList<AirOsStation>> GetStationsAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<string?> DownloadConfigAsync(CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public Task<bool> UploadConfigAsync(string configContent, CancellationToken ct = default) => throw new NotImplementedException(NotSupportedMessage);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_client is null)
            return;

        try
        {
            if (_client.IsConnected)
                _client.Disconnect();
        }
        finally
        {
            _client.Dispose();
            _client = null;
        }

        await ValueTask.CompletedTask;
    }
}