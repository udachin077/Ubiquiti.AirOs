using Ubiquiti.AirOs.Abstractions;
using Ubiquiti.AirOs.Utils;

namespace Ubiquiti.AirOs.Extensions;

/// <summary>
/// Convenience extension methods on an <see cref="IAirOsManager"/>.
/// </summary>
public static class IAirOsManagerExtensions
{
    // -------- Watchdog --------

    /// <summary>
    /// Queues enabling the watchdog with the specified host and parameters.
    /// </summary>
    /// <param name="manager">The manager instance. Must not be <see langword="null"/>.</param>
    /// <param name="host">The watchdog host IPv4 address. Must be a valid IPv4 address.</param>
    /// <param name="delay">The initial delay in seconds. Defaults to <c>300</c>.</param>
    /// <param name="period">The check period in seconds. Defaults to <c>300</c>.</param>
    /// <param name="retry">The number of retries. Defaults to <c>3</c>.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> or <paramref name="host"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="host"/> is empty or not a valid IPv4 address.</exception>
    public static IAirOsManager EnableWatchdog(this IAirOsManager manager, string host, int delay = 300, int period = 300, int retry = 3)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(host, nameof(host));

        return manager
            .SetWatchdogHost(host)
            .SetWatchdogPeriod(period)
            .SetWatchdogDelay(delay)
            .SetWatchdogRetry(retry)
            .SetWatchdogEnabled(true);
    }

    /// <summary>
    /// Queues disabling the watchdog.
    /// </summary>
    /// <param name="manager">The manager instance. Must not be <see langword="null"/>.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is <see langword="null"/>.</exception>
    public static IAirOsManager DisableWatchdog(this IAirOsManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return manager.SetWatchdogEnabled(false);
    }

    // -------- Network --------

    /// <summary>
    /// Queues a complete static network configuration (IP, netmask, gateway) and disables DHCP.
    /// </summary>
    /// <param name="manager">The manager instance. Must not be <see langword="null"/>.</param>
    /// <param name="ip">The new IPv4 address.</param>
    /// <param name="netmask">The new IPv4 subnet mask.</param>
    /// <param name="gateway">The gateway IPv4 address. Must not be empty.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ip"/>, <paramref name="netmask"/>, or <paramref name="gateway"/> is invalid.</exception>
    public static IAirOsManager SetStaticNetwork(this IAirOsManager manager, string ip, string netmask, string gateway)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(gateway, nameof(gateway));

        manager.SetDhcp(false);
        manager.SetStaticIp(ip);
        manager.SetStaticNetmask(netmask);
        manager.SetStaticGateway(gateway);

        return manager;
    }

    /// <summary>
    /// Queues enabling DHCP with a fallback IP address.
    /// </summary>
    /// <param name="manager">The manager instance. Must not be <see langword="null"/>.</param>
    /// <param name="fallbackIp">The fallback IPv4 address used if DHCP fails.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fallbackIp"/> is empty or invalid.</exception>
    public static IAirOsManager EnableDhcp(this IAirOsManager manager, string fallbackIp)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackIp, nameof(fallbackIp));
        return manager.EnableDhcpCore(fallbackIp, "255.255.255.0");
    }

    /// <summary>
    /// Queues enabling DHCP with a fallback IP address and netmask.
    /// </summary>
    /// <param name="manager">The manager instance. Must not be <see langword="null"/>.</param>
    /// <param name="fallbackIp">The fallback IPv4 address used if DHCP fails.</param>
    /// <param name="fallbackNetmask">The fallback subnet mask used if DHCP fails.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fallbackIp"/> or <paramref name="fallbackNetmask"/> is empty or invalid.</exception>
    public static IAirOsManager EnableDhcp(this IAirOsManager manager, string fallbackIp, string fallbackNetmask)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackIp, nameof(fallbackIp));
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackNetmask, nameof(fallbackNetmask));
        return manager.EnableDhcpCore(fallbackIp, fallbackNetmask);
    }

    private static IAirOsManager EnableDhcpCore(this IAirOsManager manager, string? fallbackIp, string? fallbackNetmask)
    {
        manager.SetDhcp(true);

        manager.SetStaticIp("0.0.0.0");
        manager.SetStaticNetmask("0.0.0.0");
        manager.SetStaticGateway(null);
        manager.SetStaticPrimaryDns(null);
        manager.SetStaticSecondaryDns(null);

        if (fallbackIp != null)
            manager.SetFallbackIp(fallbackIp);

        if (fallbackNetmask != null)
            manager.SetFallbackNetmask(fallbackNetmask);

        return manager;
    }

    // -------- Wireless --------

    /// <summary>
    /// Queues a complete access-point configuration.
    /// </summary>
    /// <param name="manager">The manager instance. Must not be <see langword="null"/>.</param>
    /// <param name="ssid">The SSID to broadcast.</param>
    /// <param name="password">The WPA2 pre-shared key, or <see langword="null"/> / empty to create an open network.</param>
    /// <param name="channelWidth">The channel width in MHz.</param>
    /// <param name="txPower">The transmit power in dBm.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is <see langword="null"/>.</exception>
    public static IAirOsManager SetAccessPoint(this IAirOsManager manager, string ssid, string? password, int channelWidth, int txPower)
    {
        ArgumentNullException.ThrowIfNull(manager);

        ClearAaaParams(manager);
        ClearWpaSupplicantParams(manager);

        manager.SetWirelessMode(WirelessMode.AccessPoint);
        manager.SetSsid(ssid, WirelessMode.AccessPoint);
        manager.SetChannel(channelWidth);
        manager.SetTxPower(txPower);

        if (!string.IsNullOrWhiteSpace(password))
        {
            manager.SetWpaSecurityEnabled(true, WirelessMode.AccessPoint);
            manager.SetWpaPassword(password, WirelessMode.AccessPoint);

            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("aaa.1.br.devname", "br0"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("aaa.1.devname", "ath0"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("aaa.1.driver", "madwifi"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("aaa.1.ssid", ssid));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("aaa.1.radius.macacl.status", "disabled"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("aaa.1.radius.acct.1.status", "disabled"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("aaa.1.radius.auth.1.status", "disabled"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.status", "disabled"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.device.1.status", "disabled"));
        }
        else
        {
            manager.SetWpaSecurityEnabled(false, WirelessMode.AccessPoint);
        }

        return manager;
    }

    /// <summary>
    /// Queues a complete station (client) configuration.
    /// </summary>
    /// <param name="manager">The manager instance. Must not be <see langword="null"/>.</param>
    /// <param name="ssid">The SSID to connect to.</param>
    /// <param name="password">The WPA2 pre-shared key, or <see langword="null"/> / empty for an open network.</param>
    /// <param name="channelWidth">The channel width in MHz.</param>
    /// <param name="txPower">The transmit power in dBm.</param>
    /// <returns>The same manager instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is <see langword="null"/>.</exception>
    public static IAirOsManager SetStation(this IAirOsManager manager, string ssid, string? password, int channelWidth, int txPower)
    {
        ArgumentNullException.ThrowIfNull(manager);

        ClearAaaParams(manager);
        ClearWpaSupplicantParams(manager);

        manager.SetWirelessMode(WirelessMode.Station);
        manager.SetSsid(ssid, WirelessMode.Station);
        manager.SetChannel(channelWidth);
        manager.SetTxPower(txPower);

        if (!string.IsNullOrWhiteSpace(password))
        {
            manager.SetWpaSecurityEnabled(true, WirelessMode.Station);
            manager.SetWpaPassword(password, WirelessMode.Station);

            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.device.1.devname", "ath0"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.device.1.driver", "madwifi"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.device.1.profile", "WPA-PSK"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.name", "WPA-PSK"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.network.1.bssid", ""));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.network.1.scan_ssid", "1"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.network.1.disabled", "0"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.network.1.eap.1.status", "disabled"));
            manager.AddCommand(AirOsCommandBuilder.BuildSetCommand("ebtables.sys.eap.status", "enabled"));
        }
        else
        {
            manager.SetWpaSecurityEnabled(false, WirelessMode.Station);
        }

        return manager;
    }

    private static void ClearAaaParams(IAirOsManager manager)
    {
        manager.AddCommand("grep -v '^aaa.' /tmp/system.cfg > /tmp/system.cfg.tmp && mv /tmp/system.cfg.tmp /tmp/system.cfg");
    }

    private static void ClearWpaSupplicantParams(IAirOsManager manager)
    {
        manager.AddCommand("grep -v '^wpasupplicant.' /tmp/system.cfg > /tmp/system.cfg.tmp && mv /tmp/system.cfg.tmp /tmp/system.cfg");
    }
}