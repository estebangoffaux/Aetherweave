using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zwedze.Aetherweave.SharedKernel.DomainEvents;

namespace Zwedze.Aetherweave.Application.DomainEventHandlers;

internal sealed class DomainEventDispatcher(
    ILogger<DomainEventDispatcher> logger,
    IHostApplicationLifetime hostApplicationLifetime) : IDomainEventDispatcher
{
    private readonly Dictionary<Type, List<Func<IDomainEvent, CancellationToken, Task>>> _handlers = new();

    public async Task Dispatch(IHasDomainEvents domainEventsHolder)
    {
        // The only valid cancellation token should be the one used by the application when it is shutting down.
        // This ensures that all domain events are processed before the application stops.
        // Any http request cancellation tokens should not be used here, as they may lead to incomplete processing of domain events.
        var ct = hostApplicationLifetime.ApplicationStopped;

        foreach (var domainEvent in domainEventsHolder.PopDomainEvents())
        {
            // A failing event must not prevent the next ones from being dispatched: they are already popped
            try
            {
                await DispatchAsync(domainEvent, ct);
            }
            catch (AggregateException ex)
            {
                logger.LogError(ex, "Error occurred while dispatching domain event {DomainEvent}", domainEvent.GetType().Name);
            }
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
            ct.ThrowIfCancellationRequested();
            try
            {
                await handler(@event, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
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
