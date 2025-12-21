namespace Zwedze.Aetherweave.Data.Relational.Exceptions;

public sealed class NoTransactionException() : AetherweaveDataException("There is no transaction to commit.");
