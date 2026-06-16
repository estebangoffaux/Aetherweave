using Zwedze.Aetherweave.SharedKernel.DomainEvents;

namespace Zwedze.Aetherweave.Application.DomainEventHandlers;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IHasDomainEvents domainEventsHolder, CancellationToken cancellationToken);
}
