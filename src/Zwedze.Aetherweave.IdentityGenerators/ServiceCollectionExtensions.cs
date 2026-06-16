using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zwedze.Aetherweave.IdentityGenerators.Configuration;
using Zwedze.Aetherweave.IdentityGenerators.Generator;

namespace Zwedze.Aetherweave.IdentityGenerators;

/// <summary>
///     <see cref="IServiceCollection" /> extension methods for the UidGeneration library.
/// </summary>
public static class ServiceCollectionExtensions
{
    [UsedImplicitly]
    public static IServiceCollection AddAetherweaveGenerators(
        this IServiceCollection services,
        Action<UidGenerationOptions> optionsAction)
    {
        var options = new UidGenerationOptions();
        optionsAction(options);

        services.AddSingleton<IIdentityGenerator>(sp =>
        {
            var environmentInfoRetriever = new EnvironmentInfoRetriever(sp.GetRequiredService<ILogger<EnvironmentInfoRetriever>>());
            var logger = sp.GetRequiredService<ILogger<IdentityGeneratorFactory>>();
            var instanceIdFactory = new InstanceIdFactory();
            var timeSourceFactory = new TimeSourceFactory();

            var factory = new IdentityGeneratorFactory(
                environmentInfoRetriever,
                instanceIdFactory,
                timeSourceFactory,
                logger);
            return factory.Create(options);
        });
        return services;
    }
}
