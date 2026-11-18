namespace Ubiquiti.AirOs.Models;

/// <summary>
/// Represents firmware version information.
/// </summary>
/// <param name="Platform">The platform name (e.g., "XM", "XW").</param>
/// <param name="Version">The firmware version string.</param>
public record FirmwareVersionInfo(string Platform, string Version);