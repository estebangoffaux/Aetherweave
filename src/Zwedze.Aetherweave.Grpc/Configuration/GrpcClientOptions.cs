using System.ComponentModel.DataAnnotations;

namespace Zwedze.Aetherweave.Grpc.Configuration;

public sealed record GrpcClientOptions : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    public required string Address { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Timeout <= TimeSpan.Zero)
        {
            yield return new ValidationResult("Timeout must be greater than zero", [nameof(Timeout)]);
        }

        if (!Uri.TryCreate(Address, UriKind.Absolute, out _))
        {
            yield return new ValidationResult("Address must be a valid absolute URI", [nameof(Address)]);
        }
    }
}
