using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Zwedze.Aetherweave.Security.ClientCredentials.Configuration;

namespace Zwedze.Aetherweave.Security.ClientCredentials.Test;

public class ClientCredentialsSchemeOptionsTest
{
    [Test]
    public void Validate_Should_Succeed_WhenAllFieldsAreValid()
    {
        var options = new ClientCredentialsSchemeOptions
        {
            TokenEndpoint = "https://identity.example.com/connect/token",
            ClientId = "orders-service",
            ClientSecret = "secret",
        };

        var results = Validate(options);

        results.Should().BeEmpty();
    }

    [Test]
    public void Validate_Should_Fail_WhenTokenEndpointIsNotAnAbsoluteUri()
    {
        var options = new ClientCredentialsSchemeOptions
        {
            TokenEndpoint = "not-a-uri",
            ClientId = "orders-service",
            ClientSecret = "secret",
        };

        var results = Validate(options);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(ClientCredentialsSchemeOptions.TokenEndpoint)));
    }

    private static IList<ValidationResult> Validate(ClientCredentialsSchemeOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
