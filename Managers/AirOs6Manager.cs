using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Text.Json;
using Ubiquiti.AirOs.Abstractions;
using Ubiquiti.AirOs.Models;
using Ubiquiti.AirOs.Utils;

namespace Ubiquiti.AirOs.Managers;

/// <summary>
/// SSH interaction manager for devices running airOS 6.
/// </summary>
public sealed class AirOs6Manager : IAirOsManager
{
    private readonly List<string> _pendingCommands = [];
    private readonly ILogger<AirOs6Manager> _logger;
    private readonly IIpValidator _ipValidator;
    private readonly IUbiquitiPasswordHasher _passwordHasher;
    private SshClient? _client;

    public AirOs6Manager(IIpValidator ipValidator, IUbiquitiPasswordHasher passwordHasher, ILogger<AirOs6Manager>? logger = null)
    {
        _ipValidator = ipValidator ?? throw new ArgumentNullException(nameof(ipValidator));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _logger = logger ?? NullLogger<AirOs6Manager>.Instance;
    }

    public int PendingCommandsCount => _pendingCommands.Count;

    // -------- Connection & Core Execution --------

    public async Task ConnectAsync(string ipAddress = "192.168.1.20", string username = "ubnt", string password = "ubnt", int port = 22, CancellationToken ct = default)
    {
        _client?.Dispose();
        _client = new SshClient(ipAddress, port, username, password);
        _client.HostKeyReceived += (_, e) => e.CanTrust = true;
        await _client.ConnectAsync(ct);
    }

    public async Task<CommandResult> ExecuteBatchAsync(IReadOnlyList<string> commands, CancellationToken ct = default)
    {
        EnsureConnected();
        if (commands.Count == 0)
            return new CommandResult(string.Empty, null, 0);

        var joined = string.Join(" && ", commands);
        using var cmd = _client!.CreateCommand(joined);
        await cmd.ExecuteAsync(ct);

        return new CommandResult(cmd.Result ?? string.Empty, cmd.Error, cmd.ExitStatus);
    }

    public void AddCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be empty.", nameof(command));
        _pendingCommands.Add(command);
    }

    // -------- Queue Management & Apply --------

    public void ClearCommands() => _pendingCommands.Clear();

    public async Task<bool> ChangeAsync(CancellationToken ct = default)
    {
        EnsureConnected();

        if (_pendingCommands.Count == 0)
        {
            _logger.LogInformation("No pending changes.");
            return true;
        }

        try
        {
            if (!await TryCreateBackupAsync(ct))
            {
                _logger.LogError("Could not create a configuration backup. Aborting change.");
                return false;
            }

            foreach (var cmdText in _pendingCommands)
            {
                using var cmd = _client!.CreateCommand(cmdText);
                await cmd.ExecuteAsync(ct);

                if (cmd.ExitStatus != 0)
                {
                    _logger.LogError("Command failed: {Command}\nError: {Error}", cmdText, cmd.Error);
                    return false;
                }
            }

            _pendingCommands.Clear();
            _logger.LogInformation("Changes written to /tmp/system.cfg.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change failed.");
            return false;
        }
    }

    public async Task<bool> ApplyChangesAsync(CancellationToken ct = default)
    {
        EnsureConnected();

        if (_pendingCommands.Count > 0)
        {
            _logger.LogWarning("Uncommitted commands found. Executing them before applying.");
            if (!await ChangeAsync(ct))
                return false;
        }

        try
        {
            using var flashCmd = _client!.CreateCommand("cfgmtd -w -p /etc");
            await flashCmd.ExecuteAsync(ct);

            if (flashCmd.ExitStatus != 0)
            {
                _logger.LogError("cfgmtd failed: {Error}", flashCmd.Error);
                return false;
            }

            using var restartCmd = _client.CreateCommand("/usr/etc/rc.d/rc.softrestart save");
            await restartCmd.ExecuteAsync(ct);

            return true;
        }
        catch (Exception ex) when (ex is SshException || ex is SshConnectionException)
        {
            _logger.LogInformation("SSH connection dropped during restart (expected for network changes).");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apply failed.");
            return false;
        }
    }

    public async Task<bool> DiscardAsync(CancellationToken ct = default)
    {
        EnsureConnected();

        var checkCmd = await ExecuteBatchAsync(["test -f /tmp/system.cfg.backup && echo 'exists' || echo 'not exists'"], ct);
        if (checkCmd.Output.Trim() != "exists")
        {
            _logger.LogWarning("No backup found on device.");
            _pendingCommands.Clear();
            return false;
        }

        try
        {
            using var cpCmd = _client!.CreateCommand("cp /tmp/system.cfg.backup /tmp/system.cfg");
            await cpCmd.ExecuteAsync(ct);
            if (cpCmd.ExitStatus != 0)
                throw new InvalidOperationException($"Failed to copy backup: {cpCmd.Error}");

            _pendingCommands.Clear();
            _logger.LogInformation("Backup restored to /tmp/system.cfg.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup.");
            return false;
        }
    }

    public async Task ResetToFactoryAsync(CancellationToken ct = default)
    {
        EnsureConnected();

        _logger.LogInformation("Resetting to factory defaults...");

        try
        {
            using var resetCmd = _client!.CreateCommand("cfgmtd -f /usr/etc/system.cfg -w");
            await resetCmd.ExecuteAsync(ct);

            if (resetCmd.ExitStatus != 0)
            {
                _logger.LogError("Factory reset failed with exit code {Code}: {Error}", resetCmd.ExitStatus, resetCmd.Error);
                throw new InvalidOperationException($"Factory reset failed: {resetCmd.Error}");
            }

            _logger.LogInformation("Factory reset applied, rebooting...");

            using var rebootCmd = _client.CreateCommand("reboot");
            await rebootCmd.ExecuteAsync(ct);

            if (rebootCmd.ExitStatus != 0)
            {
                _logger.LogError("Reboot failed with exit code {Code}: {Error}", rebootCmd.ExitStatus, rebootCmd.Error);
                throw new InvalidOperationException($"Reboot failed: {rebootCmd.Error}");
            }

            _pendingCommands.Clear();
            _logger.LogInformation("Device reset to factory defaults and rebooted.");
        }
        catch (Exception ex) when (ex is SshException || ex is SshConnectionException)
        {
            _logger.LogInformation("SSH connection closed (expected during reboot).");
            _pendingCommands.Clear();
        }
    }

    public async Task RebootAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        _logger.LogInformation("Sending reboot command...");

        try
        {
            using var cmd = _client!.CreateCommand("reboot");
            await cmd.ExecuteAsync(ct);

            if (cmd.ExitStatus != 0)
                _logger.LogWarning("Reboot command returned non-zero exit code: {Error}", cmd.Error);
        }
        catch (Exception ex) when (ex is SshException || ex is SshConnectionException)
        {
            _logger.LogInformation("SSH connection closed (expected during reboot).");
        }
    }

    // -------- Configuration Setters --------

    public IAirOsManager SetUsername(string username)
    {
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("users.1.name", username));
        return this;
    }

    public IAirOsManager SetPassword(string password)
    {
        var hash = _passwordHasher.HashPassword(password);
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("users.1.password", hash));
        return this;
    }

    public IAirOsManager SetHostname(string hostname)
    {
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("resolv.host.1.name", hostname));
        return this;
    }

    public IAirOsManager SetStaticIp(string ip)
    {
        if (!_ipValidator.IsValidIpAddress(ip))
            throw new ArgumentException($"Invalid IP address: {ip}", nameof(ip));
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("netconf.3.ip", ip));
        return this;
    }

    public IAirOsManager SetStaticNetmask(string netmask)
    {
        if (!_ipValidator.IsValidNetmask(netmask))
            throw new ArgumentException($"Invalid netmask: {netmask}", nameof(netmask));
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("netconf.3.netmask", netmask));
        return this;
    }

    public IAirOsManager SetDhcp(bool enabled)
    {
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("dhcpc.1.status", StatusString(enabled)));
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("netconf.3.autoip.status", StatusString(enabled)));
        return this;
    }

    public IAirOsManager SetFallbackIp(string ip)
    {
        if (!_ipValidator.IsValidIpAddress(ip))
            throw new ArgumentException($"Invalid IP address: {ip}", nameof(ip));

        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("dhcpc.1.fallback", ip));
        return this;
    }

    public IAirOsManager SetFallbackNetmask(string netmask)
    {
        if (!_ipValidator.IsValidNetmask(netmask))
            throw new ArgumentException($"Invalid netmask: {netmask}", nameof(netmask));

        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("dhcpc.1.fallback_netmask", netmask));
        return this;
    }

    public IAirOsManager SetStaticGateway(string? gateway)
    {
        if (string.IsNullOrEmpty(gateway))
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("route.1.status", "disabled"));
        }
        else
        {
            if (!_ipValidator.IsValidGateway(gateway))
                throw new ArgumentException($"Invalid gateway address: {gateway}", nameof(gateway));

            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("route.1.status", "enabled"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("route.1.ip", "0.0.0.0"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("route.1.netmask", "0"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("route.1.devname", "br0"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("route.1.comment", ""));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("route.1.gateway", gateway));
        }
        return this;
    }

    public IAirOsManager SetStaticPrimaryDns(string? dns)
    {
        if (!string.IsNullOrEmpty(dns))
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("resolv.nameserver.status", "enabled"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("resolv.nameserver.1.ip", dns));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("resolv.nameserver.1.status", "enabled"));
        }
        else
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("resolv.nameserver.status", "disabled"));
        }
        return this;
    }

    public IAirOsManager SetStaticSecondaryDns(string? dns)
    {
        if (!string.IsNullOrEmpty(dns))
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("resolv.nameserver.2.ip", dns));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("resolv.nameserver.2.status", "enabled"));
        }
        else
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("resolv.nameserver.2.status", "disabled"));
        }
        return this;
    }

    public IAirOsManager SetSsid(string ssid, WirelessMode mode)
    {
        if (mode == WirelessMode.AccessPoint)
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wireless.1.ssid", ssid));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("aaa.1.ssid", ssid));
        }
        else if (mode == WirelessMode.Station)
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wireless.1.ssid", ssid));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.network.1.ssid", ssid));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.network.1.scan_ssid", "1"));
        }
        else
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wireless.1.ssid", ssid));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("aaa.1.ssid", ssid));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.network.1.ssid", ssid));
        }
        return this;
    }

    public IAirOsManager SetWirelessMode(WirelessMode mode)
    {
        switch (mode)
        {
            case WirelessMode.AccessPoint:
                _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("radio.1.mode", "master"));
                _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("radio.1.cwm.mode", "2"));
                break;
            case WirelessMode.Station:
                _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("radio.1.mode", "managed"));
                _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("radio.1.cwm.mode", "1"));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
        return this;
    }

    public IAirOsManager SetChannel(int channelWidth)
    {
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("radio.1.chanbw", channelWidth.ToString()));
        return this;
    }

    public IAirOsManager SetTxPower(int power)
    {
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("radio.1.txpower", power.ToString()));
        return this;
    }

    public IAirOsManager SetWatchdogEnabled(bool enabled)
    {
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("pwdog.status", StatusString(enabled)));
        return this;
    }

    public IAirOsManager SetWatchdogHost(string host)
    {
        if (!_ipValidator.IsValidIpAddress(host))
            throw new ArgumentException($"Invalid watchdog host address: {host}", nameof(host));

        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("pwdog.host", host));
        return this;
    }

    public IAirOsManager SetWatchdogPeriod(int period)
    {
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("pwdog.period", period.ToString()));
        return this;
    }

    public IAirOsManager SetWatchdogDelay(int delay)
    {
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("pwdog.delay", delay.ToString()));
        return this;
    }

    public IAirOsManager SetWatchdogRetry(int retry)
    {
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("pwdog.retry", retry.ToString()));
        return this;
    }

    public IAirOsManager SetWpaSecurityEnabled(bool enabled, WirelessMode mode)
    {
        if (!enabled)
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wireless.1.security.type", "none"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wireless.1.authmode", "0"));

            if (mode == WirelessMode.AccessPoint)
                _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("aaa.status", "disabled"));
            else
                _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.status", "disabled"));
        }
        else
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wireless.1.security.type", "wpa-psk"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wireless.1.authmode", "1"));

            if (mode == WirelessMode.AccessPoint)
                _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("aaa.status", "enabled"));
            else
                _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.status", "enabled"));
        }
        return this;
    }

    public IAirOsManager SetWpaPassword(string password, WirelessMode mode)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wireless.1.security.type", "wpa-psk"));
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wireless.1.authmode", "1"));

        if (mode == WirelessMode.AccessPoint)
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("aaa.status", "enabled"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("aaa.1.status", "enabled"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("aaa.1.wpa.psk", password));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("aaa.1.wpa.mode", "2"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("aaa.1.wpa.1.pairwise", "CCMP"));
        }
        else
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.status", "enabled"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.network.1.psk", password));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.network.1.key_mgmt.1.name", "WPA-PSK"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.network.1.pairwise.1.name", "CCMP"));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("wpasupplicant.profile.1.network.1.proto.1.name", "RSN"));
        }
        return this;
    }

    public IAirOsManager SetCountry(int countryCode)
    {
        if (countryCode > 0)
        {
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("system.countrycode", countryCode.ToString()));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("radio.countrycode", countryCode.ToString()));
            _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("radio.1.countrycode", countryCode.ToString()));
        }
        return this;
    }

    public IAirOsManager AcceptEula(DateTime? acceptedDate = null)
    {
        var date = acceptedDate ?? DateTime.Now;
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("system.eula.accepted", "enabled"));
        _pendingCommands.Add(AirOsCommandBuilder.BuildSetCommand("system.eula.accepted.date", date.ToString("yyyy-MM-dd")));
        return this;
    }

    // -------- Configuration Getters --------

    public Task<string?> GetHostnameAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("grep '^resolv.host.1.name=' /tmp/system.cfg | cut -d= -f2", ct);

    public Task<string?> GetStaticIpAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("grep '^netconf.3.ip=' /tmp/system.cfg | cut -d= -f2", ct);

    public Task<string?> GetStaticNetmaskAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("grep '^netconf.3.netmask=' /tmp/system.cfg | cut -d= -f2", ct);

    public Task<string?> GetFallbackIpAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("grep '^dhcpc.1.fallback=' /tmp/system.cfg | cut -d= -f2", ct);

    public Task<string?> GetFallbackNetmaskAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("grep '^dhcpc.1.fallback_netmask=' /tmp/system.cfg | cut -d= -f2", ct);

    public Task<string?> GetStaticGatewayAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("grep '^route.1.gateway=' /tmp/system.cfg | cut -d= -f2", ct);

    public Task<string?> GetStaticPrimaryDnsAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("grep '^resolv.nameserver.1.ip=' /tmp/system.cfg | cut -d= -f2", ct);

    public Task<string?> GetStaticSecondaryDnsAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("grep '^resolv.nameserver.2.ip=' /tmp/system.cfg | cut -d= -f2", ct);

    public async Task<bool?> GetDhcpAsync(CancellationToken ct = default)
    {
        var status = await ExecuteReadCommandAsync("grep '^netconf.3.autoip.status=' /tmp/system.cfg | cut -d= -f2", ct);
        return status is null ? null : status.Equals("enabled", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<DhcpInfo?> GetDhcpInfoAsync(CancellationToken ct = default)
    {
        var raw = await ExecuteReadCommandAsync("cat /etc/udhcpc/info.br0", ct);

        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return DhcpInfoHelper.ParseDhcpInfo(raw);
    }

    public Task<string?> GetSsidAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("grep '^wireless.1.ssid=' /tmp/system.cfg | cut -d= -f2", ct);

    public async Task<WirelessMode?> GetWirelessModeAsync(CancellationToken ct = default)
    {
        var mode = await ExecuteReadCommandAsync("grep '^radio.1.mode=' /tmp/system.cfg | cut -d= -f2", ct);
        if (mode is null)
            return null;

        if (mode.Equals("master", StringComparison.OrdinalIgnoreCase))
            return WirelessMode.AccessPoint;
        if (mode.Equals("managed", StringComparison.OrdinalIgnoreCase))
            return WirelessMode.Station;

        return null;
    }

    public async Task<int?> GetChannelWidthAsync(CancellationToken ct = default)
    {
        var val = await ExecuteReadCommandAsync("grep '^radio.1.chanbw=' /tmp/system.cfg | cut -d= -f2", ct);
        return val is not null && int.TryParse(val, out var res) ? res : null;
    }

    public async Task<int?> GetTxPowerAsync(CancellationToken ct = default)
    {
        var val = await ExecuteReadCommandAsync("grep '^radio.1.txpower=' /tmp/system.cfg | cut -d= -f2", ct);
        return val is not null && int.TryParse(val, out var res) ? res : null;
    }

    public async Task<bool> GetWpaSecurityEnabledAsync(CancellationToken ct = default)
    {
        var mode = await GetWirelessModeAsync(ct);
        if (mode is null)
            return false;

        string command = mode == WirelessMode.AccessPoint
            ? "grep '^aaa.status=' /tmp/system.cfg | cut -d= -f2"
            : "grep '^wpasupplicant.status=' /tmp/system.cfg | cut -d= -f2";

        var result = await ExecuteReadCommandAsync(command, ct);
        return result?.Equals("enabled", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task<string?> GetWpaPasswordAsync(CancellationToken ct = default)
    {
        var mode = await GetWirelessModeAsync(ct);
        if (mode is null)
            return null;

        string command = mode == WirelessMode.AccessPoint
            ? "grep '^aaa.1.wpa.psk=' /tmp/system.cfg | cut -d= -f2"
            : "grep '^wpasupplicant.profile.1.network.1.psk=' /tmp/system.cfg | cut -d= -f2";

        return await ExecuteReadCommandAsync(command, ct);
    }

    public async Task<int?> GetCountryCodeAsync(CancellationToken ct = default)
    {
        var val = await ExecuteReadCommandAsync("grep '^system.countrycode=' /tmp/system.cfg | cut -d= -f2", ct);

        if (string.IsNullOrEmpty(val))
            val = await ExecuteReadCommandAsync("grep '^radio.countrycode=' /tmp/system.cfg | cut -d= -f2", ct);

        if (string.IsNullOrEmpty(val))
            val = await ExecuteReadCommandAsync("grep '^radio.1.countrycode=' /tmp/system.cfg | cut -d= -f2", ct);

        return val is not null && int.TryParse(val, out var res) ? res : null;
    }

    public async Task<string?> GetCountryNameAsync(CancellationToken ct = default)
    {
        var code = await GetCountryCodeAsync(ct);
        return code.HasValue ? CountryCodeHelper.GetCountryName(code.Value) : null;
    }

    public async Task<CountryInfo?> GetCountryInfoAsync(CancellationToken ct = default)
    {
        var code = await GetCountryCodeAsync(ct);
        if (!code.HasValue)
            return null;
        return new CountryInfo(code, CountryCodeHelper.GetCountryName(code.Value));
    }

    public async Task<bool?> GetEulaAcceptedAsync(CancellationToken ct = default)
    {
        var val = await ExecuteReadCommandAsync("grep '^system.eula.accepted=' /tmp/system.cfg | cut -d= -f2", ct);
        return val is null ? null : val.Equals("enabled", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<DateTime?> GetEulaAcceptedDateAsync(CancellationToken ct = default)
    {
        var val = await ExecuteReadCommandAsync("grep '^system.eula.accepted.date=' /tmp/system.cfg | cut -d= -f2", ct);
        return val is not null && DateTime.TryParse(val, out var date) ? date : null;
    }

    public async Task<bool?> GetWatchdogEnabledAsync(CancellationToken ct = default)
    {
        var status = await ExecuteReadCommandAsync("grep '^pwdog.status=' /tmp/system.cfg | cut -d= -f2", ct);
        return status is null ? null : status.Equals("enabled", StringComparison.OrdinalIgnoreCase);
    }

    public Task<string?> GetWatchdogHostAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("grep '^pwdog.host=' /tmp/system.cfg | cut -d= -f2", ct);

    public async Task<int?> GetWatchdogPeriodAsync(CancellationToken ct = default)
    {
        var val = await ExecuteReadCommandAsync("grep '^pwdog.period=' /tmp/system.cfg | cut -d= -f2", ct);
        return val is not null && int.TryParse(val, out var res) ? res : null;
    }

    public async Task<int?> GetWatchdogDelayAsync(CancellationToken ct = default)
    {
        var val = await ExecuteReadCommandAsync("grep '^pwdog.delay=' /tmp/system.cfg | cut -d= -f2", ct);
        return val is not null && int.TryParse(val, out var res) ? res : null;
    }

    public async Task<int?> GetWatchdogRetryAsync(CancellationToken ct = default)
    {
        var val = await ExecuteReadCommandAsync("grep '^pwdog.retry=' /tmp/system.cfg | cut -d= -f2", ct);
        return val is not null && int.TryParse(val, out var res) ? res : null;
    }

    public async Task<string?> GetSystemLogAsync(int lines = 100, CancellationToken ct = default)
    {
        EnsureConnected();
        var command = $"cat /var/log/messages 2>/dev/null | tail -n {lines} || logread | tail -n {lines}";
        var result = await ExecuteBatchAsync([command], ct);
        if (!result.Success && string.IsNullOrWhiteSpace(result.Output))
            return null;
        return result.Output;
    }

    public Task<string?> GetFullConfigAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("cat /tmp/system.cfg", ct);

    public async Task<FirmwareVersionInfo?> GetFirmwareVersionAsync(CancellationToken ct = default)
    {
        EnsureConnected();

        var output = await ExecuteBatchAsync(["mca-status | head -n 1"], ct);
        if (output.Success && output.Output.Contains("Firmware Version:"))
        {
            try
            {
                var parts = output.Output.Split("Firmware Version:")[1].Trim().Split('.');
                if (parts.Length >= 2)
                {
                    var platform = parts[0].Trim();
                    var version = string.Join(".", parts.Skip(1)).Trim();
                    if (version.StartsWith('v'))
                        version = version[1..];
                    return new FirmwareVersionInfo(platform, version);
                }
            }
            catch { }
        }

        var fallback = await ExecuteBatchAsync(["cat /etc/version"], ct);
        if (fallback.Success && !string.IsNullOrWhiteSpace(fallback.Output))
        {
            try
            {
                var parts = fallback.Output.Trim().Split('.');
                if (parts.Length >= 2)
                {
                    var platform = parts[0].Trim();
                    var version = string.Join(".", parts.Skip(1)).Trim();
                    if (version.StartsWith('v'))
                        version = version[1..];
                    return new FirmwareVersionInfo(platform, version);
                }
            }
            catch { }
        }

        return null;
    }

    // -------- Firmware Management --------

    public async Task<bool> UpgradeFirmwareAsync(byte[] firmwareContent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(firmwareContent);
        if (firmwareContent.Length == 0)
            throw new ArgumentException("Firmware content cannot be empty.", nameof(firmwareContent));

        using var stream = new MemoryStream(firmwareContent);
        return await UpgradeFirmwareAsync(stream, ct);
    }

    public async Task<bool> UpgradeFirmwareAsync(Stream firmwareStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(firmwareStream);
        EnsureConnected();

        _logger.LogInformation("Starting firmware upgrade process...");

        try
        {
            _logger.LogInformation("Uploading firmware to /tmp/fwupdate.bin...");
            using var uploadCmd = _client!.CreateCommand("cat > /tmp/fwupdate.bin");
            uploadCmd.CommandTimeout = TimeSpan.FromSeconds(120);

            var executeTask = uploadCmd.ExecuteAsync(ct);

            using (var inputStream = uploadCmd.CreateInputStream())
            {
                await firmwareStream.CopyToAsync(inputStream, ct);
                await inputStream.FlushAsync(ct);
                inputStream.Close();
            }

            await executeTask;

            if (uploadCmd.ExitStatus != 0)
            {
                _logger.LogError("Failed to upload firmware: {Error}", uploadCmd.Error);
                return false;
            }

            _logger.LogInformation("Verifying firmware upload...");
            var verifyCmd = await ExecuteBatchAsync(["ls -la /tmp/fwupdate.bin | awk '{print $5}'"], ct);
            if (!verifyCmd.Success || string.IsNullOrEmpty(verifyCmd.Output))
            {
                _logger.LogError("Firmware file not found after upload.");
                return false;
            }

            _logger.LogInformation("Firmware uploaded successfully, size: {Size} bytes.", verifyCmd.Output.Trim());
            _logger.LogInformation("Installing firmware using ubntbox fwupdate.real...");
            var installCmd = _client.CreateCommand("ubntbox fwupdate.real -m /tmp/fwupdate.bin");
            installCmd.CommandTimeout = TimeSpan.FromSeconds(180);

            try
            {
                await installCmd.ExecuteAsync(ct);

                if (installCmd.ExitStatus != 0)
                {
                    _logger.LogError("Firmware installation failed with exit code: {ExitCode}. Error: {Error}", installCmd.ExitStatus, installCmd.Error);
                    return false;
                }

                _logger.LogInformation("Firmware installation completed successfully.");
                return true;
            }
            catch (Exception ex) when (ex is SshException || ex is SshConnectionException)
            {
                _logger.LogInformation("SSH connection closed (expected during reboot). Firmware upgrade assumed successful.");
                return true;
            }
        }
        catch (Exception ex) when (ex is not SshException && ex is not SshConnectionException)
        {
            _logger.LogError(ex, "Firmware upgrade failed due to an unexpected error.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("SSH connection closed (expected during reboot). Firmware upgrade assumed successful.");
            return true;
        }
    }

    // -------- Monitoring --------

    public async Task<AirOsRadioStatus?> GetRadioStatusAsync(CancellationToken ct = default)
    {
        var result = await ExecuteBatchAsync(["mca-status"], ct);

        if (!result.Success)
            throw new InvalidOperationException($"mca-status failed with exit code {result.ExitStatus}: {result.Error}");

        var status = AirOsRadioStatus.Parse(result.Output);
        if (status is null) return null;

        var cfgResult = await ExecuteBatchAsync(["grep -E '^(aaa\\.1|wpasupplicant|radio\\.1)' /tmp/system.cfg"], ct);
        if (cfgResult.Success && !string.IsNullOrWhiteSpace(cfgResult.Output))
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in cfgResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Trim().Split('=', 2);
                if (parts.Length == 2) dict[parts[0]] = parts[1];
            }

            string security = "none";
            string radioMode = dict.GetValueOrDefault("radio.1.mode", "");

            if (radioMode == "master")
            {
                if (dict.GetValueOrDefault("aaa.1.status") == "enabled")
                {
                    string mode = dict.GetValueOrDefault("aaa.1.wpa.mode", "2");
                    string pairwise = dict.GetValueOrDefault("aaa.1.wpa.1.pairwise", "CCMP");

                    string version = mode == "2" ? "WPA2" : "WPA";
                    string cipher = pairwise.Equals("CCMP", StringComparison.OrdinalIgnoreCase) ? "AES" : pairwise;
                    security = $"{version}-{cipher}";
                }
            }
            else if (radioMode == "managed")
            {
                if (dict.GetValueOrDefault("wpasupplicant.status") == "enabled")
                {
                    string proto = dict.GetValueOrDefault("wpasupplicant.profile.1.network.1.proto.1.name", "");
                    string pairwise = dict.GetValueOrDefault("wpasupplicant.profile.1.network.1.pairwise.1.name", "");

                    if (!string.IsNullOrEmpty(proto))
                    {
                        string version = proto.Equals("RSN", StringComparison.OrdinalIgnoreCase) ? "WPA2" : "WPA";
                        string cipher = pairwise.Equals("CCMP", StringComparison.OrdinalIgnoreCase) ? "AES" : pairwise;
                        security = $"{version}-{cipher}";
                    }
                }
            }

            status = status with { Security = security };
        }

        return status;
    }

    public async Task<IReadOnlyList<AirOsStation>> GetStationsAsync(CancellationToken ct = default)
    {
        var result = await ExecuteBatchAsync(["wstalist"], ct);

        if (!result.Success)
            throw new InvalidOperationException($"wstalist failed with exit code {result.ExitStatus}: {result.Error}");

        var json = result.Output;
        if (string.IsNullOrWhiteSpace(json) || !json.Trim().StartsWith("["))
            return Array.Empty<AirOsStation>();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<AirOsStation>>(json, options);
            return list ?? (IReadOnlyList<AirOsStation>)Array.Empty<AirOsStation>();
        }
        catch (JsonException)
        {
            return Array.Empty<AirOsStation>();
        }
    }

    // -------- Private Helpers --------

    private static string StatusString(bool enabled) => enabled ? "enabled" : "disabled";

    private void EnsureConnected()
    {
        if (_client is null || !_client.IsConnected)
            throw new InvalidOperationException("SSH client is not connected. Call ConnectAsync first.");
    }

    private async Task<string?> ExecuteReadCommandAsync(string command, CancellationToken ct = default)
    {
        var result = await ExecuteBatchAsync([command], ct);
        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
            return null;
        return result.Output.Trim();
    }

    private async Task<bool> TryCreateBackupAsync(CancellationToken ct = default)
    {
        try
        {
            var checkCmd = await ExecuteBatchAsync(["test -f /tmp/system.cfg && echo 'exists' || echo 'not exists'"], ct);
            if (checkCmd.Output.Trim() != "exists")
            {
                _logger.LogWarning("/tmp/system.cfg not found, cannot create a backup.");
                return false;
            }

            using var cpCmd = _client!.CreateCommand("cp /tmp/system.cfg /tmp/system.cfg.backup");
            await cpCmd.ExecuteAsync(ct);

            if (cpCmd.ExitStatus != 0)
            {
                _logger.LogWarning("Failed to copy /tmp/system.cfg to /tmp/system.cfg.backup: {Error}", cpCmd.Error);
                return false;
            }

            _logger.LogInformation("Backup created: /tmp/system.cfg.backup");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create a backup; proceeding without one.");
            return false;
        }
    }

    public Task<string?> DownloadConfigAsync(CancellationToken ct = default)
        => ExecuteReadCommandAsync("cat /tmp/system.cfg", ct);

    public async Task<bool> UploadConfigAsync(string configContent, CancellationToken ct = default)
    {
        EnsureConnected();
        ArgumentException.ThrowIfNullOrWhiteSpace(configContent, nameof(configContent));

        try
        {
            _logger.LogInformation("Uploading configuration to /tmp/system.cfg (size: {Size} bytes)...", configContent.Length);

            using var uploadCmd = _client!.CreateCommand("cat > /tmp/system.cfg");
            var executeTask = uploadCmd.ExecuteAsync(ct);

            using (var inputStream = uploadCmd.CreateInputStream())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(configContent);
                await inputStream.WriteAsync(bytes, 0, bytes.Length, ct);
                await inputStream.FlushAsync(ct);
                inputStream.Close();
            }

            await executeTask;

            if (uploadCmd.ExitStatus != 0)
            {
                _logger.LogError("Failed to write config to /tmp/system.cfg: {Error}", uploadCmd.Error);
                return false;
            }

            using (var rmCmd = _client.CreateCommand("rm -f /tmp/system.cfg.backup"))
            {
                await rmCmd.ExecuteAsync(ct);
            }

            using var cfgmtdCmd = _client.CreateCommand("cfgmtd -w -p /etc");
            await cfgmtdCmd.ExecuteAsync(ct);

            if (cfgmtdCmd.ExitStatus != 0)
            {
                _logger.LogError("Failed to save config to flash: {Error}", cfgmtdCmd.Error);
                return false;
            }

            _logger.LogInformation("Configuration saved to flash successfully. Rebooting device...");

            await RebootAsync(ct);

            _pendingCommands.Clear();
            _logger.LogInformation("Configuration uploaded. Device is rebooting.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload configuration.");
            return false;
        }
    }

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