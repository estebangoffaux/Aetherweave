using System.Collections.Immutable;

namespace Zwedze.Aetherweave.SharedKernel.DomainEvents;

public interface IHasDomainEvents
{
    ImmutableArray<IDomainEvent> PopDomainEvents();
}
