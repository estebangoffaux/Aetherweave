using System.ComponentModel.DataAnnotations;

namespace Zwedze.Aetherweave.Http.Configuration;

public sealed record ClientCredentialsSchemeOptions : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    public required string TokenEndpoint { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string ClientId { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string ClientSecret { get; init; }

    public string? Scope { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(TokenEndpoint, UriKind.Absolute, out _))
        {
            yield return new ValidationResult("TokenEndpoint must be a valid absolute URI", [nameof(TokenEndpoint)]);
        }
    }
}
