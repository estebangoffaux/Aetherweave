namespace Zwedze.Aetherweave.Data.Relational.Exceptions;

public sealed class TransactionNotCommittedException() : AetherweaveDataException(
    "Transaction was not committed before disposal. " +
    "Call Commit() or allow automatic rollback.");
