using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ubiquiti.AirOs.Abstractions;
using Ubiquiti.AirOs.Managers;
using Ubiquiti.AirOs.Options;
using Ubiquiti.AirOs.Security;
using Ubiquiti.AirOs.Validation;

namespace Ubiquiti.AirOs.Extensions;

/// <summary>
/// Provides extension methods for registering airOS services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default airOS services (IP validator, password hasher, and manager factory)
    /// along with the default versions (airOS 6 and 8).
    /// </summary>
    /// <param name="services">The service collection to register services in.</param>
    /// <param name="configure">Optional callback to register additional versions or override existing ones.</param>
    /// <returns>The same service collection for method chaining.</returns>
    public static IServiceCollection AddAirOs(this IServiceCollection services, Action<AirOsManagerOptions>? configure = null)
    {
        services.TryAddSingleton<IIpValidator, IpValidator>();
        services.TryAddSingleton<IUbiquitiPasswordHasher, MD5PasswordHasher>();

        services.AddOptions<AirOsManagerOptions>()
            .Configure(options =>
            {
                options.Register<AirOs6Manager>("6");
                options.Register<AirOs8Manager>("8");
            });

        if (configure != null)
            services.Configure(configure);

        services.TryAddSingleton<IAirOsManagerFactory, AirOsManagerFactory>();

        return services;
    }

    /// <summary>
    /// Replaces the default manager factory implementation.
    /// </summary>
    /// <typeparam name="TFactory">The custom factory type implementing <see cref="IAirOsManagerFactory"/>.</typeparam>
    /// <param name="services">The service collection to register services in.</param>
    /// <returns>The same service collection for method chaining.</returns>
    public static IServiceCollection AddAirOsFactory<TFactory>(this IServiceCollection services)
        where TFactory : class, IAirOsManagerFactory
    {
        services.Replace(ServiceDescriptor.Singleton<IAirOsManagerFactory, TFactory>());
        return services;
    }
}