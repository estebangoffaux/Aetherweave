using System.ComponentModel.DataAnnotations;

namespace Zwedze.Aetherweave.Data.Relational;

public sealed record DataRelationalOptions
{
    [Required(AllowEmptyStrings = false)] public required string ConnectionStringName { get; init; }

    public bool EnableDetailedErrors { get; init; } = true;
    public bool EnableSensitiveDataLogging { get; init; }
    public bool NoTrackingAsDefaultTrackingStrategy { get; init; } = true;
}
