using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Zwedze.Aetherweave.Security.Oidc.Native.Configuration;

namespace Zwedze.Aetherweave.Security.Oidc.Native.Test;

public class PkceSchemeOptionsTest
{
    [Test]
    public void Validate_Should_Succeed_WhenAllFieldsAreValid()
    {
        var options = new PkceSchemeOptions
        {
            Authority = "https://identity.example.com",
            ClientId = "desktop-client",
            RedirectUri = "http://127.0.0.1:7890/callback",
        };

        var results = Validate(options);

        results.Should().BeEmpty();
    }

    [Test]
    public void Validate_Should_Fail_WhenAuthorityIsNotAnAbsoluteUri()
    {
        var options = new PkceSchemeOptions
        {
            Authority = "not-a-uri",
            ClientId = "desktop-client",
            RedirectUri = "http://127.0.0.1:7890/callback",
        };

        var results = Validate(options);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(PkceSchemeOptions.Authority)));
    }

    [Test]
    public void Validate_Should_Fail_WhenRedirectUriIsNotAnAbsoluteUri()
    {
        var options = new PkceSchemeOptions
        {
            Authority = "https://identity.example.com",
            ClientId = "desktop-client",
            RedirectUri = "not-a-uri",
        };

        var results = Validate(options);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(PkceSchemeOptions.RedirectUri)));
    }

    [Test]
    public void Scope_Should_DefaultToOpenIdProfileOfflineAccess()
    {
        var options = new PkceSchemeOptions
        {
            Authority = "https://identity.example.com",
            ClientId = "desktop-client",
            RedirectUri = "http://127.0.0.1:7890/callback",
        };

        options.Scope.Should().Be("openid profile offline_access");
    }

    private static IList<ValidationResult> Validate(PkceSchemeOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
