namespace Ubiquiti.AirOs.Abstractions;

/// <summary>
/// Provides a factory for creating airOS managers for a specific firmware version.
/// </summary>
public interface IAirOsManagerFactory
{
    /// <summary>Creates a manager instance for the specified airOS version.</summary>
    /// <param name="version">The airOS version key (e.g. "6", "8").</param>
    /// <returns>An instance implementing <see cref="IAirOsManager"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown when the version is not registered.</exception>
    IAirOsManager Create(string version);
}