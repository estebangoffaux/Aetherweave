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

        // Register handlers
        services.AddTransient<ProfilingHandler>();
        services.AddTransient<ContentTracingHandler>();

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

    extension(IHttpClientBuilder builder)
    {
        [UsedImplicitly]
        public IHttpClientBuilder AddAetherweaveHandler<THandler>()
            where THandler : DelegatingHandler
        {
            builder.Services.AddTransient<THandler>();
            return builder.AddHttpMessageHandler<THandler>();
        }

        [UsedImplicitly]
        public IHttpClientBuilder AddAetherweaveErrorHandler<TErrorHandler>()
            where TErrorHandler : class, IHttpErrorHandler
        {
            builder.Services.AddTransient<IHttpErrorHandler, TErrorHandler>();
            builder.Services.AddTransient<HttpErrorResponseHandler>();
            return builder.AddHttpMessageHandler<HttpErrorResponseHandler>();
        }
    }
}
