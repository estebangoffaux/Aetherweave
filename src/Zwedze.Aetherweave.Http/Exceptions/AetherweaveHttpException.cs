namespace Zwedze.Aetherweave.Http.Exceptions;

public abstract class AetherweaveHttpException : Exception
{
    protected AetherweaveHttpException(string message)
        : base(message)
    {
    }

    protected AetherweaveHttpException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
