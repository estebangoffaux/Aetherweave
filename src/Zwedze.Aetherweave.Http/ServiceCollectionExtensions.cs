using Duende.AccessTokenManagement;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zwedze.Aetherweave.Http.Configuration;
using Zwedze.Aetherweave.Http.Exceptions;
using Zwedze.Aetherweave.Http.Handlers;

namespace Zwedze.Aetherweave.Http;

public static class ServiceCollectionExtensions
{
    [UsedImplicitly]
    public static IHttpClientBuilder AddAetherweaveHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration,
        string clientName,
        string sectionName = "Aetherweave:HttpClients")
        where TClient : class
        where TImplementation : class, TClient
    {
        var fullSectionName = $"{sectionName}:{clientName}";

        var clientSection = configuration.GetSection(fullSectionName);
        if (!clientSection.Exists())
        {
            throw new ConfigurationNotFoundException(fullSectionName);
        }

        // Register options with validation
        services
            .AddOptions<HttpClientOptions>(clientName)
            .Bind(clientSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure HttpClient
        var builder = services.AddHttpClient<TClient, TImplementation>(
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
    public static IServiceCollection AddAetherweaveOpenIdConnectAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Aetherweave:Authentication")
    {
        var authenticationSection = configuration.GetSection(sectionName);
        if (!authenticationSection.Exists())
        {
            throw new ConfigurationNotFoundException(sectionName);
        }

        RegisterClientCredentialsSchemes(services, authenticationSection.GetSection("ClientCredentials"));
        RegisterPkceSchemes(services, authenticationSection.GetSection("Pkce"));

        return services;
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

            services
                .AddOptions<ClientCredentialsSchemeOptions>(schemeName)
                .Bind(schemeSection)
                .ValidateDataAnnotations()
                .ValidateOnStart();

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

    private static void RegisterPkceSchemes(IServiceCollection services, IConfigurationSection pkceSection)
    {
        foreach (var schemeSection in pkceSection.GetChildren())
        {
            services
                .AddOptions<PkceSchemeOptions>(schemeSection.Key)
                .Bind(schemeSection)
                .ValidateDataAnnotations()
                .ValidateOnStart();
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

        [UsedImplicitly]
        public IHttpClientBuilder WithUserAccessTokenAuthentication(string schemeName)
        {
            return builder.AddHttpMessageHandler(sp =>
                new PkceAuthenticationHandler(
                    sp.GetRequiredService<IOptionsMonitor<PkceSchemeOptions>>(),
                    sp,
                    schemeName));
        }
    }
}
