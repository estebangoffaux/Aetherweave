using Zwedze.Aetherweave.SharedKernel.DomainEvents;

namespace Zwedze.Aetherweave.Application.DomainEventHandlers;

public interface IDomainEventDispatcher
{
    Task Dispatch(IHasDomainEvents domainEventsHolder);
}
