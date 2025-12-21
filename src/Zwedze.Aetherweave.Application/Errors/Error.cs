using System.Collections.Immutable;

namespace Zwedze.Aetherweave.Application.Errors;

public record Error
{
    public const string UnmanagedErrorCode = "__unmanaged_error";

    public required ImmutableArray<string> Codes { get; init; }
    public required string Message { get; init; }
}
