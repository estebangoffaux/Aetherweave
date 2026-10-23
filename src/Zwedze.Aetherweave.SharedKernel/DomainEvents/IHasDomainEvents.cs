namespace Zwedze.Aetherweave.SharedKernel.DomainEvents;

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> PopDomainEvents();
}
