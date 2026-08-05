using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Zwedze.Aetherweave.Grpc.Configuration;

namespace Zwedze.Aetherweave.Grpc.Test;

public class GrpcClientOptionsTest
{
    [Test]
    public void Validate_Should_Succeed_WhenAllFieldsAreValid()
    {
        var options = new GrpcClientOptions
        {
            Address = "https://greeter.example.com",
        };

        var results = Validate(options);

        results.Should().BeEmpty();
    }

    [Test]
    public void Validate_Should_Fail_WhenAddressIsNotAnAbsoluteUri()
    {
        var options = new GrpcClientOptions
        {
            Address = "not-a-uri",
        };

        var results = Validate(options);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(GrpcClientOptions.Address)));
    }

    [Test]
    public void Validate_Should_Fail_WhenTimeoutIsZeroOrNegative()
    {
        var options = new GrpcClientOptions
        {
            Address = "https://greeter.example.com",
            Timeout = TimeSpan.Zero,
        };

        var results = Validate(options);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(GrpcClientOptions.Timeout)));
    }

    private static IList<ValidationResult> Validate(GrpcClientOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
