namespace Zwedze.Aetherweave.Data.Relational.Exceptions;

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
