using System.Globalization;
using Ubiquiti.AirOs.Models;

namespace Ubiquiti.AirOs.Extensions;

/// <summary>
/// Provides display-oriented formatting helpers for <see cref="AirOsStation"/>.
/// </summary>
public static class AirOsStationDisplayExtensions
{
    /// <summary>Formats the uptime as <c>hh:mm:ss</c>.</summary>
    public static string ToUptimeDisplay(this AirOsStation station)
        => TimeSpan.FromSeconds(station.Uptime).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

    /// <summary>Formats the signal strength.</summary>
    public static string ToSignalDisplay(this AirOsStation station)
        => $"{station.Signal} dBm";

    /// <summary>Formats the TX/RX rate.</summary>
    public static string ToRateDisplay(this AirOsStation station)
        => $"{station.Tx} / {station.Rx} Mbps";

    /// <summary>Formats the CCQ.</summary>
    public static string ToCcqDisplay(this AirOsStation station)
        => $"{station.Ccq} %";

    /// <summary>Formats the SNR, or "-" if unavailable.</summary>
    public static string ToSnrDisplay(this AirOsStation station)
        => station.Snr is null ? "-" : $"{station.Snr} dB";
}