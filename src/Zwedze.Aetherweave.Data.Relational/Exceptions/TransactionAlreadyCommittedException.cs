namespace Zwedze.Aetherweave.Data.Relational.Exceptions;

public sealed class TransactionAlreadyCommittedException() : AetherweaveDataException("Transaction is already committed.");
