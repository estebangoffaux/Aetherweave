namespace Zwedze.Aetherweave.Data.Exceptions;

public sealed class TransactionAlreadyRolledBackException() : AetherweaveDataException(
    "Transaction is already rolled back. " +
    "Create a new unit of work to persist further changes.");
