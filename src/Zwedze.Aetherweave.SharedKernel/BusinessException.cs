namespace Zwedze.Aetherweave.SharedKernel;

public abstract class BusinessException(ErrorCode errorCode, string message) : ApplicationException(message)
{
    public ErrorCode ErrorCode { get; } = errorCode;
}
