using System.ComponentModel.DataAnnotations;

namespace Zwedze.Aetherweave.Http.Configuration;

public sealed record PkceSchemeOptions : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    public required string Authority { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string ClientId { get; init; }

    public string? ClientSecret { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string RedirectUri { get; init; }

    public string Scope { get; init; } = "openid profile offline_access";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(Authority, UriKind.Absolute, out _))
        {
            yield return new ValidationResult("Authority must be a valid absolute URI", [nameof(Authority)]);
        }

        if (!Uri.TryCreate(RedirectUri, UriKind.Absolute, out _))
        {
            yield return new ValidationResult("RedirectUri must be a valid absolute URI", [nameof(RedirectUri)]);
        }
    }
}
