namespace Ubiquiti.AirOs;

/// <summary>
/// Defines the wireless operation mode for Ubiquiti devices.
/// </summary>
public enum WirelessMode
{
    /// <summary>
    /// Device operates as a station (client mode).
    /// Connects to an existing access point.
    /// </summary>
    Station = 1,

    /// <summary>
    /// Device operates as an access point.
    /// Hosts a wireless network for other devices to connect.
    /// </summary>
    AccessPoint = 2
}