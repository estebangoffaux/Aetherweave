using Duende.AccessTokenManagement;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zwedze.Aetherweave.Core.Configurations.Exceptions;
using Zwedze.Aetherweave.Security.ClientCredentials.Configuration;

namespace Zwedze.Aetherweave.Security.ClientCredentials;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        [UsedImplicitly]
        public IServiceCollection AddAetherweaveClientCredentialsAuthentication(
            IConfiguration configuration,
            string sectionName = "Aetherweave:Security:ClientCredentials")
        {
            var clientCredentialsSection = configuration.GetSection(sectionName);
            if (!clientCredentialsSection.Exists())
            {
                throw new ConfigurationNotFoundException(sectionName);
            }

            RegisterClientCredentialsSchemes(services, clientCredentialsSection);

            return services;
        }
    }

    private static void RegisterClientCredentialsSchemes(IServiceCollection services, IConfigurationSection clientCredentialsSection)
    {
        var schemeSections = clientCredentialsSection.GetChildren().ToArray();
        if (schemeSections.Length == 0)
        {
            return;
        }

        var tokenManagementBuilder = services.AddClientCredentialsTokenManagement();

        foreach (var schemeSection in schemeSections)
        {
            var schemeName = schemeSection.Key;

            tokenManagementBuilder.AddClient(
                schemeName,
                client =>
                {
                    var options = schemeSection.Get<ClientCredentialsSchemeOptions>()!;

                    client.TokenEndpoint = new Uri(options.TokenEndpoint);
                    client.ClientId = ClientId.Parse(options.ClientId);
                    client.ClientSecret = ClientSecret.Parse(options.ClientSecret);

                    if (!string.IsNullOrWhiteSpace(options.Scope))
                    {
                        client.Scope = Scope.Parse(options.Scope);
                    }
                });
        }
    }

    extension(IHttpClientBuilder builder)
    {
        [UsedImplicitly]
        public IHttpClientBuilder WithClientCredentialsAuthentication(string schemeName)
        {
            return builder
                .AddDefaultAccessTokenResiliency()
                .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse(schemeName));
        }
    }
}
