using System.Globalization;
using Ubiquiti.AirOs.Models;

namespace Ubiquiti.AirOs.Extensions;

/// <summary>
/// Provides display-oriented formatting helpers for <see cref="AirOsRadioStatus"/>.
/// </summary>
public static class AirOsRadioStatusDisplayExtensions
{
    private const int SignalMin = -94;
    private const int SignalMax = -65;

    /// <summary>Formats the system uptime as <c>hh:mm:ss</c>.</summary>
    public static string ToUptimeDisplay(this AirOsRadioStatus status)
        => TimeSpan.FromSeconds(status.Uptime).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

    /// <summary>Formats the wireless link uptime as <c>hh:mm:ss</c>.</summary>
    public static string ToLinkUptimeDisplay(this AirOsRadioStatus status)
        => TimeSpan.FromSeconds(status.WlanUptime).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

    /// <summary>Formats the AP MAC address, or "Not Associated".</summary>
    public static string ToApMacDisplay(this AirOsRadioStatus status)
        => status.IsAssociated && !string.IsNullOrEmpty(status.ApMac) && status.ApMac != "00:00:00:00:00:00"
            ? status.ApMac
            : "Not Associated";

    /// <summary>Formats the signal strength, or "-" if not associated.</summary>
    public static string ToSignalDisplay(this AirOsRadioStatus status)
        => status.IsAssociated ? $"{status.Signal} dBm" : "-";

    /// <summary>Formats the noise level, or "-" if not associated.</summary>
    public static string ToNoiseDisplay(this AirOsRadioStatus status)
        => status.IsAssociated ? $"{status.Noise} dBm" : "-";

    /// <summary>Formats the Chain 0 signal, or "-" if not associated.</summary>
    public static string ToChain0Display(this AirOsRadioStatus status)
        => status.IsAssociated ? $"{status.Chain0Signal} dBm" : "-";

    /// <summary>Formats the Chain 1 signal, or "-" if not associated.</summary>
    public static string ToChain1Display(this AirOsRadioStatus status)
        => status.IsAssociated ? $"{status.Chain1Signal} dBm" : "-";

    /// <summary>Formats the normalized CCQ, or "-" if not associated.</summary>
    public static string ToCcqDisplay(this AirOsRadioStatus status)
        => status.IsAssociated ? $"{status.NormalizedCcq} %" : "-";

    /// <summary>Formats the TX/RX rate.</summary>
    public static string ToRateDisplay(this AirOsRadioStatus status)
        => status.IsAssociated ? $"{status.WlanTxRate} / {status.WlanRxRate} Mbps" : "- / -";

    /// <summary>Formats the TX latency.</summary>
    public static string ToLatencyDisplay(this AirOsRadioStatus status)
        => status.WlanTxLatency > 0 ? $"{status.WlanTxLatency} ms" : "-";

    /// <summary>Formats the SNR, or "-" if not associated.</summary>
    public static string ToSnrDisplay(this AirOsRadioStatus status)
        => status.Snr is null ? "-" : $"{status.Snr} dB";

    /// <summary>Formats the airMAX status.</summary>
    public static string ToAirMaxDisplay(this AirOsRadioStatus status)
        => status.WlanPolling > 0 ? "Enabled" : "Disabled";

    /// <summary>Formats the airMAX quality.</summary>
    public static string ToAirMaxQualityDisplay(this AirOsRadioStatus status)
        => status.WlanPolling > 0 ? $"{status.WlanPollingQuality} %" : "-";

    /// <summary>Formats the airMAX capacity.</summary>
    public static string ToAirMaxCapacityDisplay(this AirOsRadioStatus status)
        => status.WlanPolling > 0 ? $"{status.WlanPollingCapacity} %" : "-";

    /// <summary>Formats the LAN plug status.</summary>
    public static string ToLanPluggedDisplay(this AirOsRadioStatus status)
        => status.LanPlugged > 0 ? "Plugged" : "Unplugged";

    /// <summary>Formats the wireless error counters.</summary>
    public static string ToWlanErrorsDisplay(this AirOsRadioStatus status)
        => $"Rx {status.WlanRxErrors} / Tx {status.WlanTxErrors}";

    /// <summary>Formats the LAN error counters.</summary>
    public static string ToLanErrorsDisplay(this AirOsRadioStatus status)
        => $"Rx {status.LanRxErrors} / Tx {status.LanTxErrors}";

    /// <summary>Formats the signal percentage for a UI gradient bar.</summary>
    public static double ToSignalPercent(this AirOsRadioStatus status)
    {
        if (!status.IsAssociated) return 0;
        int s = status.Signal;
        if (s >= SignalMax) return 100;
        if (s <= SignalMin) return 0;
        double percent = (s - SignalMin) / (double)(SignalMax - SignalMin) * 100.0;
        return Math.Round(Math.Clamp(percent, 0, 100));
    }

    /// <summary>Formats the firmware version as <c>vX.Y.Z (Platform)</c>.</summary>
    public static string ToFirmwareVersionDisplay(this AirOsRadioStatus status)
    {
        if (string.IsNullOrWhiteSpace(status.FirmwareVersion))
            return "-";

        var parts = status.FirmwareVersion.Split('.');
        if (parts.Length >= 3)
        {
            var platform = parts[0];
            var versionIndex = Array.FindIndex(parts, p => p.StartsWith("v"));
            if (versionIndex > 0)
            {
                var version = string.Join(".", parts.Skip(versionIndex).Take(3));
                return $"{version} ({platform})";
            }
        }

        return status.FirmwareVersion;
    }

    /// <summary>Formats the wireless operation mode.</summary>
    public static string ToWlanOpmodeDisplay(this AirOsRadioStatus status)
        => status.WlanOpmode?.ToLowerInvariant() switch
        {
            "sta" => "Station",
            "ap" => "Access Point",
            _ => status.WlanOpmode ?? "-"
        };

    /// <summary>Formats the network role.</summary>
    public static string ToNetroleDisplay(this AirOsRadioStatus status)
        => status.Netrole?.ToLowerInvariant() switch
        {
            "bridge" => "Bridge",
            "router" => "Router",
            "nat" => "NAT",
            _ => status.Netrole ?? "-"
        };

    /// <summary>Formats the security mode.</summary>
    public static string ToSecurityDisplay(this AirOsRadioStatus status)
    {
        if (string.IsNullOrWhiteSpace(status.Security))
            return "-";

        if (status.Security.Contains('-'))
            return status.Security;

        return status.Security.ToLowerInvariant() switch
        {
            "none" => "None",
            "wep" => "WEP",
            "wpa" => "WPA",
            "wpa2" => "WPA2",
            "wpapsk" => "WPA-PSK",
            "wpa2psk" => "WPA2-PSK",
            _ => status.Security
        };
    }
}