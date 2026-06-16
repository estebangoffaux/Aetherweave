namespace Zwedze.Aetherweave.SharedKernel.DomainEvents;

/// <summary>
///     Handler for a specific domain event type
///     Implement this interface and the source generator will automatically register it
/// </summary>
/// <typeparam name="TEvent">The domain event type.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
