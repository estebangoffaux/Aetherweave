using JetBrains.Annotations;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zwedze.Aetherweave.Core.Configurations;
using Zwedze.Aetherweave.Http;
using Zwedze.Aetherweave.Security.Oidc.Configuration;

namespace Zwedze.Aetherweave.Security.Oidc;

public static class ServiceCollectionExtensions
{
    public static OidcHttpClientBuilder AddAetherweaveOidcHttpClients(this IServiceCollection services, IEnumerable<string> authorizedUrls, string sectionName = "Aetherweave:Security:Oidc")
    {
        var builder = new OidcHttpClientBuilder(services, authorizedUrls);
        return builder;
    }

    public static IServiceCollection AddAetherweaveOidcClientAuthentication(this IServiceCollection services, IConfiguration configuration, string sectionName = "Aetherweave:Security:Oidc")
    {
        var oidcOptions = ConfigurationLoader.GetOptions<AetherweaveOidcOptions>(configuration, sectionName);
        services.AddOidcAuthentication(o =>
        {
            o.ProviderOptions.ClientId = oidcOptions.ClientId;
            o.ProviderOptions.Authority = oidcOptions.Authority;
            o.ProviderOptions.ResponseType = oidcOptions.ResponseType;
            o.ProviderOptions.RedirectUri = oidcOptions.RedirectUri;
            o.ProviderOptions.PostLogoutRedirectUri = oidcOptions.PostLogoutRedirectUri;
            foreach (var scope in oidcOptions.DefaultScopes)
            {
                o.ProviderOptions.DefaultScopes.Add(scope);
            }
        });

        return services;
    }

    extension(IHttpClientBuilder builder)
    {
        [UsedImplicitly]
        public IHttpClientBuilder WithAuth(IEnumerable<string> authorizedUrls)
        {
            return builder.AddHttpMessageHandler(sp => sp
                .GetRequiredService<AuthorizationMessageHandler>()
                .ConfigureHandler(authorizedUrls));
        }
    }
}

public class OidcHttpClientBuilder
{
    private readonly IEnumerable<string> _authorizedUrls;
    private readonly IServiceCollection _services;

    internal OidcHttpClientBuilder(IServiceCollection services, IEnumerable<string> authorizedUrls)
    {
        _services = services;
        _authorizedUrls = authorizedUrls;
    }

    public IHttpClientBuilder AddAetherweaveHttpClient<TInterface, TImplementation>(IConfiguration configuration, string clientName)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        var builder = _services.AddAetherweaveHttpClient<TInterface, TImplementation>(configuration, clientName);
        return builder.AddHttpMessageHandler(sp => sp
            .GetRequiredService<AuthorizationMessageHandler>()
            .ConfigureHandler(_authorizedUrls));
    }
}
