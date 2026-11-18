namespace Ubiquiti.AirOs.Models;

/// <summary>
/// Represents a wireless station (client) connected to the device.
/// Contains raw values only; use display extensions for formatted output.
/// </summary>
public sealed record AirOsStation
{
    /// <summary>Gets the MAC address of the station.</summary>
    public string Mac { get; init; } = string.Empty;

    /// <summary>Gets the name of the station.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the last known IP address of the station.</summary>
    public string LastIp { get; init; } = string.Empty;

    /// <summary>Gets the transmit rate in Mbps.</summary>
    public double Tx { get; init; }

    /// <summary>Gets the receive rate in Mbps.</summary>
    public double Rx { get; init; }

    /// <summary>Gets the signal strength in dBm.</summary>
    public int Signal { get; init; }

    /// <summary>Gets the noise floor in dBm.</summary>
    public int NoiseFloor { get; init; }

    /// <summary>Gets the client connection quality (CCQ) in percent.</summary>
    public int Ccq { get; init; }

    /// <summary>Gets the uptime in seconds.</summary>
    public int Uptime { get; init; }

    /// <summary>
    /// Gets the Signal-to-Noise Ratio in dB, or <see langword="null"/> when unavailable.
    /// </summary>
    public int? Snr => Signal > -96 && NoiseFloor > -96 ? Signal - NoiseFloor : null;
}