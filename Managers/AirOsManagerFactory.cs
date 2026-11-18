using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ubiquiti.AirOs.Abstractions;
using Ubiquiti.AirOs.Options;

namespace Ubiquiti.AirOs.Managers;

/// <summary>
/// Default implementation of <see cref="IAirOsManagerFactory"/> that resolves managers
/// from a registry populated in the DI container.
/// </summary>
public sealed class AirOsManagerFactory : IAirOsManagerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AirOsManagerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AirOsManagerFactory"/> class.
    /// </summary>
    public AirOsManagerFactory(IServiceProvider serviceProvider, IOptions<AirOsManagerOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public IAirOsManager Create(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be empty.", nameof(version));

        if (!_options.Managers.TryGetValue(version, out var managerType))
            throw new NotSupportedException($"airOS version '{version}' is not registered.");

        return (IAirOsManager)ActivatorUtilities.CreateInstance(_serviceProvider, managerType);
    }
}