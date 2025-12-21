namespace Zwedze.Aetherweave.SharedKernel.DomainEvents;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
}

/// <summary>
///     Base record for domain events with default implementations
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
