namespace Zwedze.Aetherweave.SharedKernel.DomainEvents;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
}
