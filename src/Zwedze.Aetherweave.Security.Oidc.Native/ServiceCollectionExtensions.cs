using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zwedze.Aetherweave.Core.Configurations;
using Zwedze.Aetherweave.Core.Configurations.Exceptions;
using Zwedze.Aetherweave.Security.Oidc.Native.Configuration;
using Zwedze.Aetherweave.Security.Oidc.Native.Handlers;

namespace Zwedze.Aetherweave.Security.Oidc.Native;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        [UsedImplicitly]
        public IServiceCollection AddAetherweaveNativeOidcAuthentication(
            IConfiguration configuration,
            string sectionName = "Aetherweave:Security:Oidc:Native")
        {
            var section = configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                throw new ConfigurationNotFoundException(sectionName);
            }

            ConfigurationLoader.RegisterOptions<PkceSchemeOptions>(services, section.GetChildren());

            return services;
        }
    }

    extension(IHttpClientBuilder builder)
    {
        [UsedImplicitly]
        public IHttpClientBuilder WithNativeOidcAuthentication(string schemeName)
        {
            return builder.AddHttpMessageHandler(
                sp => new PkceAuthenticationHandler(
                    sp.GetRequiredService<IOptionsMonitor<PkceSchemeOptions>>(),
                    sp,
                    schemeName));
        }
    }
}
