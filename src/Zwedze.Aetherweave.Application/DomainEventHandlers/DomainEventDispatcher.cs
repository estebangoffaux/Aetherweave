using Microsoft.Extensions.Logging;
using Zwedze.Aetherweave.SharedKernel.DomainEvents;

namespace Zwedze.Aetherweave.Application.DomainEventHandlers;

internal sealed class DomainEventDispatcher(ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    private readonly Dictionary<Type, List<Func<IDomainEvent, CancellationToken, Task>>> _handlers = new();

    public async Task DispatchAsync(IHasDomainEvents domainEventsHolder, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEventsHolder.PopDomainEvents())
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }

    public void AddHandler<TEvent>(Func<IDomainEventHandler<TEvent>> handlerFactory)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);

        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = [];
        }

        _handlers[eventType]
            .Add(async (@event, ct) =>
            {
                var handler = handlerFactory();
                await handler.HandleAsync((TEvent)@event, ct);
            });
    }

    private async Task DispatchAsync(IDomainEvent @event, CancellationToken ct)
    {
        var eventType = @event.GetType();

        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            logger.LogDebug("No handlers registered for event {EventType}", eventType.Name);
            return;
        }

        // Collect all exceptions to ensure all handlers run
        // This supports eventual consistency and independent side effects
        var exceptions = new List<Exception>();
        foreach (var handler in handlers)
        {
            try
            {
                await handler(@event, ct);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count != 0)
        {
            throw new AggregateException(exceptions);
        }
    }
}
