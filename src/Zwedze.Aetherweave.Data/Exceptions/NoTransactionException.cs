namespace Zwedze.Aetherweave.Data.Exceptions;

public sealed class NoTransactionException() : AetherweaveDataException("There is no transaction to commit.");
