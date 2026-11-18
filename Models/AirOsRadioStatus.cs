using System.Globalization;

namespace Ubiquiti.AirOs.Models;

/// <summary>
/// Represents the radio and system status of an airOS device.
/// Contains raw values only; use display extensions for formatted output.
/// </summary>
public sealed record AirOsRadioStatus(
    string DeviceName,
    string DeviceId,
    string Platform,
    string FirmwareVersion,
    string DeviceIp,
    string BoardCrc,
    string CfgCrc,
    string Netrole,
    string WlanOpmode,
    string Essid,
    string Security,
    string ApMac,
    int Frequency,
    int CenterFrequency,
    int ChannelWidth,
    int TxPower,
    int AckTimeout,
    int Distance,
    int Signal,
    int Noise,
    int Ccq,
    int Chain0Signal,
    int Chain1Signal,
    int WlanConnections,
    int WlanUptime,
    double WlanTxRate,
    double WlanRxRate,
    int WlanTxLatency,
    int WlanPolling,
    int WlanPollingQuality,
    int WlanPollingCapacity,
    int WlanRxErrors,
    int WlanTxErrors,
    int LanRxErrors,
    int LanTxErrors,
    int WlanRxErrNwid,
    int WlanRxErrCrypt,
    int WlanRxErrFrag,
    int WlanRxErrRetries,
    int WlanRxErrBmiss,
    int WlanRxErrOther,
    string LanSpeed,
    int LanPlugged,
    long LanRxBytes,
    long LanTxBytes,
    long LanRxPackets,
    long LanTxPackets,
    int Uptime,
    double CpuUsage,
    double LoadAvg,
    int MemTotal,
    int MemFree,
    int MemBuffers,
    int StatusFlags)
{
    /// <summary>
    /// Gets a value indicating whether the device is associated with an access point.
    /// </summary>
    public bool IsAssociated => WlanConnections > 0;

    /// <summary>
    /// Gets the normalized CCQ value in percent.
    /// </summary>
    /// <remarks>
    /// airOS 6 reports CCQ multiplied by 10 (e.g. 991 = 99.1%).
    /// </remarks>
    public double NormalizedCcq => Ccq > 100 ? Ccq / 10.0 : Ccq;

    /// <summary>
    /// Gets the Signal-to-Noise Ratio in dB, or <see langword="null"/> when not associated.
    /// </summary>
    public int? Snr => IsAssociated ? Signal - Noise : null;

    /// <summary>
    /// Gets the memory usage percentage.
    /// </summary>
    public double MemUsagePercent => MemTotal > 0
        ? Math.Round((double)(MemTotal - MemFree) / MemTotal * 100)
        : 0;

    /// <summary>
    /// Parses the raw output of the <c>mca-status</c> command.
    /// </summary>
    /// <param name="raw">The raw string output from the device.</param>
    /// <returns>A parsed <see cref="AirOsRadioStatus"/>, or <see langword="null"/> if parsing fails.</returns>
    public static AirOsRadioStatus? Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            foreach (var pair in line.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = pair.IndexOf('=');
                if (idx > 0)
                    map[pair[..idx].Trim()] = pair[(idx + 1)..].Trim();
            }

        if (!map.ContainsKey("signal"))
            return null;

        string S(string k, string def = "") => map.TryGetValue(k, out var v) ? v : def;
        int I(string k, int def = 0) => int.TryParse(S(k), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : def;
        long L(string k, long def = 0) => long.TryParse(S(k), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : def;
        double D(string k, double def = 0) => double.TryParse(S(k), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;

        return new AirOsRadioStatus(
            DeviceName: S("deviceName"), DeviceId: S("deviceId"), Platform: S("platform"),
            FirmwareVersion: S("firmwareVersion"), DeviceIp: S("deviceIp"), BoardCrc: S("boardCrc"),
            CfgCrc: S("cfgCrc"), Netrole: S("netrole"),
            WlanOpmode: S("wlanOpmode"), Essid: S("essid"), Security: S("security"),
            ApMac: S("apMac"), Frequency: I("freq"), CenterFrequency: I("centerFreq"),
            ChannelWidth: I("chanbw"), TxPower: I("txPower"), AckTimeout: I("ackTimeout"),
            Distance: I("distance"),
            Signal: I("signal"), Noise: I("noise"), Ccq: I("ccq"),
            Chain0Signal: I("chain0Signal"), Chain1Signal: I("chain1Signal"), WlanConnections: I("wlanConnections"),
            WlanUptime: I("wlanUptime"), WlanTxRate: D("wlanTxRate"), WlanRxRate: D("wlanRxRate"),
            WlanTxLatency: I("wlanTxLatency"),
            WlanPolling: I("wlanPolling"), WlanPollingQuality: I("wlanPollingQuality"), WlanPollingCapacity: I("wlanPollingCapacity"),
            WlanRxErrors: I("wlanRxErrors"), WlanTxErrors: I("wlanTxErrors"), LanRxErrors: I("lanRxErrors"),
            LanTxErrors: I("lanTxErrors"), WlanRxErrNwid: I("wlanRxErrNwid"), WlanRxErrCrypt: I("wlanRxErrCrypt"),
            WlanRxErrFrag: I("wlanRxErrFrag"), WlanRxErrRetries: I("wlanRxErrRetries"), WlanRxErrBmiss: I("wlanRxErrBmiss"),
            WlanRxErrOther: I("wlanRxErrOther"),
            LanSpeed: S("lanSpeed"), LanPlugged: I("lanPlugged"), LanRxBytes: L("lanRxBytes"),
            LanTxBytes: L("lanTxBytes"), LanRxPackets: L("lanRxPackets"), LanTxPackets: L("lanTxPackets"),
            Uptime: I("uptime"), CpuUsage: D("cpuUsage"), LoadAvg: D("loadavg"),
            MemTotal: I("memTotal"), MemFree: I("memFree"), MemBuffers: I("memBuffers"),
            StatusFlags: I("status_flags"));
    }
}