using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zwedze.Aetherweave.Core.Configurations.Exceptions;
using Zwedze.Aetherweave.Grpc.Test.Protos;
using Zwedze.Aetherweave.Security.ClientCredentials;

namespace Zwedze.Aetherweave.Grpc.Test;

public class AddAetherweaveGrpcClientTest
{
    [Test]
    public void Should_Throw_WhenGrpcClientSectionIsMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddAetherweaveGrpcClient<Greeter.GreeterClient>(configuration, "GreeterService");

        act.Should().Throw<ConfigurationNotFoundException>();
    }

    [Test]
    public void Should_RegisterClient_WhenConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aetherweave:GrpcClients:GreeterService:Address"] = "https://greeter.example.com",
                ["Aetherweave:GrpcClients:GreeterService:Timeout"] = "00:00:15",
            })
            .Build();

        services.AddAetherweaveGrpcClient<Greeter.GreeterClient>(configuration, "GreeterService");
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<Greeter.GreeterClient>().Should().NotBeNull();
    }

    [Test]
    public void Should_ComposeWithClientCredentialsAuthentication()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aetherweave:GrpcClients:GreeterService:Address"] = "https://greeter.example.com",
                ["Aetherweave:Security:ClientCredentials:greeter-api:TokenEndpoint"] = "https://identity.example.com/connect/token",
                ["Aetherweave:Security:ClientCredentials:greeter-api:ClientId"] = "greeter-service",
                ["Aetherweave:Security:ClientCredentials:greeter-api:ClientSecret"] = "secret",
            })
            .Build();

        services.AddAetherweaveClientCredentialsAuthentication(configuration);
        services.AddAetherweaveGrpcClient<Greeter.GreeterClient>(configuration, "GreeterService")
            .WithClientCredentialsAuthentication("greeter-api");

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<Greeter.GreeterClient>().Should().NotBeNull();
    }
}
