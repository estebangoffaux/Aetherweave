using System.ComponentModel.DataAnnotations;

namespace Zwedze.Aetherweave.Http.Configuration;

public sealed record HttpClientOptions : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    public required string BaseAddress { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public bool EnableProfiling { get; init; }
    public bool EnableContentTracing { get; init; }
    public int MaxContentLogSize { get; init; } = 10_000;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Timeout <= TimeSpan.Zero)
        {
            yield return new ValidationResult("Timeout must be greater than zero", [nameof(Timeout)]);
        }

        if (!Uri.TryCreate(BaseAddress, UriKind.Absolute, out _))
        {
            yield return new ValidationResult("BaseAddress must be a valid absolute URI", [nameof(BaseAddress)]);
        }
    }
}
