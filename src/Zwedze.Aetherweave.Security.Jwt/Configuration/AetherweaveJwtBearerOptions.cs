using System.ComponentModel.DataAnnotations;

namespace Zwedze.Aetherweave.Security.Configuration;

public sealed record AetherweaveJwtBearerOptions
{
    [Required(AllowEmptyStrings = false)] public required string Authority { get; init; }
    [Required(AllowEmptyStrings = false)] public required string Audience { get; init; }
    [Required] public required bool RequireHttpsMetadata { get; init; }
}
