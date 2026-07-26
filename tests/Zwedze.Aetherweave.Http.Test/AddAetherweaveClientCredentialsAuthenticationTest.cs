using AwesomeAssertions;
using Duende.AccessTokenManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zwedze.Aetherweave.Core.Configurations.Exceptions;

namespace Zwedze.Aetherweave.Http.Test;

public class AddAetherweaveClientCredentialsAuthenticationTest
{
    [Test]
    public void Should_Throw_WhenClientCredentialsSectionIsMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddAetherweaveClientCredentialsAuthentication(configuration);

        act.Should().Throw<ConfigurationNotFoundException>();
    }

    [Test]
    public void Should_RegisterNothingExtra_WhenNoSchemesAreConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aetherweave:Security:ClientCredentials"] = "",
            })
            .Build();

        services.AddAetherweaveClientCredentialsAuthentication(configuration);
        var provider = services.BuildServiceProvider();

        provider.GetService<IClientCredentialsTokenManager>().Should().BeNull();
    }

    [Test]
    public void Should_RegisterClientCredentialsTokenManager_WhenASchemeIsConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aetherweave:Security:ClientCredentials:orders-api:TokenEndpoint"] = "https://identity.example.com/connect/token",
                ["Aetherweave:Security:ClientCredentials:orders-api:ClientId"] = "orders-service",
                ["Aetherweave:Security:ClientCredentials:orders-api:ClientSecret"] = "secret",
            })
            .Build();

        services.AddAetherweaveClientCredentialsAuthentication(configuration);
        var provider = services.BuildServiceProvider();

        provider.GetService<IClientCredentialsTokenManager>().Should().NotBeNull();
    }
}
