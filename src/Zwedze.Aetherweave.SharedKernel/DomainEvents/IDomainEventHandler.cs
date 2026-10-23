namespace Zwedze.Aetherweave.SharedKernel.DomainEvents;

/// <summary>
///     Handler for a specific domain event type
///     Register the implementation in DI and map it with AddAetherweaveDomainEventDispatcher
/// </summary>
/// <typeparam name="TEvent">The domain event type.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}
