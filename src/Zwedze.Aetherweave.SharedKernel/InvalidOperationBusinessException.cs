namespace Zwedze.Aetherweave.SharedKernel;

public sealed class InvalidOperationBusinessException(string message)
    : BusinessException(message, "INVALID_OPERATION")
{
}
