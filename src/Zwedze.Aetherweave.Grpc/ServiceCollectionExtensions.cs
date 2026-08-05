using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zwedze.Aetherweave.Core.Configurations;
using Zwedze.Aetherweave.Grpc.Configuration;

namespace Zwedze.Aetherweave.Grpc;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        [UsedImplicitly]
        public IHttpClientBuilder AddAetherweaveGrpcClient<TClient>(
            IConfiguration configuration,
            string clientName,
            string sectionName = "Aetherweave:GrpcClients")
            where TClient : class
        {
            var fullSectionName = $"{sectionName}:{clientName}";

            ConfigurationLoader.RegisterOptions<GrpcClientOptions>(services, configuration, fullSectionName, clientName);

            var builder = services.AddGrpcClient<TClient>(
                clientName,
                (sp, options) =>
                {
                    var grpcClientOptions = sp.GetRequiredService<IOptionsMonitor<GrpcClientOptions>>().Get(clientName);
                    options.Address = new Uri(grpcClientOptions.Address);
                });

            builder.ConfigureHttpClient((sp, client) =>
            {
                var grpcClientOptions = sp.GetRequiredService<IOptionsMonitor<GrpcClientOptions>>().Get(clientName);
                client.Timeout = grpcClientOptions.Timeout;
            });

            return builder;
        }
    }
}
