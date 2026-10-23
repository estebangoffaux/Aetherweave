using Zwedze.Aetherweave.SharedKernel;

namespace Zwedze.Aetherweave.Application.Errors;

public record Error
{
    public required IReadOnlyCollection<ErrorCode> Codes { get; init; }
    public required string Message { get; init; }
}
