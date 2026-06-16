namespace Zwedze.Aetherweave.SharedKernel;

public abstract class BusinessException(string message, string errorCode) : ApplicationException(message)
{
    public string ErrorCode { get; } = errorCode;
}
