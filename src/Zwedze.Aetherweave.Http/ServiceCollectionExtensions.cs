using Duende.AccessTokenManagement;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zwedze.Aetherweave.Core.Configurations;
using Zwedze.Aetherweave.Core.Configurations.Exceptions;
using Zwedze.Aetherweave.Http.Configuration;
using Zwedze.Aetherweave.Http.Handlers;

namespace Zwedze.Aetherweave.Http;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        [UsedImplicitly]
        public IHttpClientBuilder AddAetherweaveHttpClient<TInterface, TImplementation>(
            IConfiguration configuration,
            string clientName,
            string sectionName = "Aetherweave:HttpClients")
            where TInterface : class
            where TImplementation : class, TInterface
        {
            var fullSectionName = $"{sectionName}:{clientName}";

            ConfigurationLoader.RegisterOptions<HttpClientOptions>(services, configuration, fullSectionName, clientName);

            // Configure HttpClient
            var builder = services.AddHttpClient<TInterface, TImplementation>(
                clientName,
                (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptionsMonitor<HttpClientOptions>>().Get(clientName);

                    client.BaseAddress = new Uri(options.BaseAddress);
                    client.Timeout = options.Timeout;
                });

            // Add handlers conditionally based on options
            builder.AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptionsMonitor<HttpClientOptions>>().Get(clientName);
                var logger = sp.GetRequiredService<ILogger<ProfilingHandler>>();
                return new ProfilingHandler(logger, options.EnableProfiling);
            });

            builder.AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptionsMonitor<HttpClientOptions>>().Get(clientName);
                var logger = sp.GetRequiredService<ILogger<ContentTracingHandler>>();
                return new ContentTracingHandler(logger, options.EnableContentTracing, options.MaxContentLogSize);
            });

            return builder;
        }

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
        public IHttpClientBuilder WithHandler<THandler>()
            where THandler : DelegatingHandler
        {
            builder.Services.AddTransient<THandler>();
            return builder.AddHttpMessageHandler<THandler>();
        }

        [UsedImplicitly]
        public IHttpClientBuilder WithErrorHandler<TErrorHandler>()
            where TErrorHandler : class, IHttpErrorHandler
        {
            builder.Services.AddTransient<IHttpErrorHandler, TErrorHandler>();
            builder.Services.AddTransient<HttpErrorResponseHandler>();
            return builder.AddHttpMessageHandler<HttpErrorResponseHandler>();
        }

        [UsedImplicitly]
        public IHttpClientBuilder WithClientCredentialsAuthentication(string schemeName)
        {
            return builder
                .AddDefaultAccessTokenResiliency()
                .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse(schemeName));
        }
    }
}
