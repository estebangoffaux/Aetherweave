using AwesomeAssertions;
using Duende.AccessTokenManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zwedze.Aetherweave.Http.Exceptions;

namespace Zwedze.Aetherweave.Http.Test;

public class AddAetherweaveOpenIdConnectAuthenticationTest
{
    [Test]
    public void Should_Throw_WhenAuthenticationSectionIsMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddAetherweaveOpenIdConnectAuthentication(configuration);

        act.Should().Throw<ConfigurationNotFoundException>();
    }

    [Test]
    public void Should_RegisterNothingExtra_WhenNoSchemesAreConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aetherweave:Authentication:ClientCredentials"] = null,
                ["Aetherweave:Authentication:Pkce"] = null,
            })
            .Build();

        services.AddAetherweaveOpenIdConnectAuthentication(configuration);
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
                ["Aetherweave:Authentication:ClientCredentials:orders-api:TokenEndpoint"] = "https://identity.example.com/connect/token",
                ["Aetherweave:Authentication:ClientCredentials:orders-api:ClientId"] = "orders-service",
                ["Aetherweave:Authentication:ClientCredentials:orders-api:ClientSecret"] = "secret",
            })
            .Build();

        services.AddAetherweaveOpenIdConnectAuthentication(configuration);
        var provider = services.BuildServiceProvider();

        provider.GetService<IClientCredentialsTokenManager>().Should().NotBeNull();
    }
}
