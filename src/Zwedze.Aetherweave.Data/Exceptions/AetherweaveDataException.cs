namespace Zwedze.Aetherweave.Data.Exceptions;

public abstract class AetherweaveDataException : Exception
{
    protected AetherweaveDataException(string message)
        : base(message)
    {
    }

    protected AetherweaveDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
