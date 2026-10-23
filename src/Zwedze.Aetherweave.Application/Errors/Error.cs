using Zwedze.Aetherweave.SharedKernel;

namespace Zwedze.Aetherweave.Application.Errors;

public sealed record Error
{
    public required IReadOnlyCollection<ErrorCode> Codes { get; init; }
    public required string Message { get; init; }
}
