using Ubiquiti.AirOs.Abstractions;

namespace Ubiquiti.AirOs.Options;

/// <summary>
/// Options for the airOS manager factory, containing the registry of supported versions.
/// </summary>
public sealed class AirOsManagerOptions
{
    /// <summary>
    /// Gets the dictionary mapping airOS version strings (e.g. "6", "8") to manager types.
    /// </summary>
    public Dictionary<string, Type> Managers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a manager type for a specific airOS version.
    /// </summary>
    /// <typeparam name="TManager">The manager type.</typeparam>
    /// <param name="version">The version string (e.g. "6").</param>
    public void Register<TManager>(string version) where TManager : class, IAirOsManager
    {
        Managers[version] = typeof(TManager);
    }
}