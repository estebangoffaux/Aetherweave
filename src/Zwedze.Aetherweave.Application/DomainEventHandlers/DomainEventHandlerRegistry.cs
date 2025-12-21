using JetBrains.Annotations;
using Zwedze.Aetherweave.Application.Extensions;
using Zwedze.Aetherweave.SharedKernel.DomainEvents;

namespace Zwedze.Aetherweave.Application.DomainEventHandlers;

/// <summary>
/// A registry for managing and configuring domain event handlers. This class is responsible
/// for registering event handlers and providing the necessary configurations for handling
/// domain events within an application.
/// </summary>
public sealed class DomainEventHandlerRegistry
{
    /// <summary>
    /// Stores a collection of domain event handler registrations.
    /// </summary>
    /// <remarks>
    /// This collection is used internally within the <see cref="DomainEventHandlerRegistry"/>
    /// to manage and organize event handler registrations, enabling the dispatching
    /// of domain events to their appropriate handlers.
    /// </remarks>
    private readonly List<IEventHandlerRegistration> _registrations = [];

    /// Configures the registration of handlers for a specific domain event type.
    /// This method allows specifying handlers for a given domain event type and provides a fluent
    /// API to further configure other domain events.
    /// <typeparam name="TEvent">The type of the domain event to configure.</typeparam>
    /// <returns>A configurator for the specified domain event type, enabling the addition of handlers and further configuration for other events.</returns>
    public EventHandlerConfigurator<TEvent> Configure<TEvent>()
        where TEvent : IDomainEvent
    {
        return new EventHandlerConfigurator<TEvent>(this);
    }

    /// Registers a domain event handler for a specific event type.
    /// This method associates an event handler type with a domain event type.
    /// When the specified domain event is triggered, the associated handler will be invoked to process the event.
    /// <typeparam name="TEvent">The type of the domain event.</typeparam>
    /// <typeparam name="THandler">The type of the handler that handles the specified domain event.</typeparam>
    /// <returns>Returns the instance of <see cref="DomainEventHandlerRegistry"/> to allow method chaining.</returns>
    [UsedImplicitly]
    public DomainEventHandlerRegistry AddHandler<TEvent, THandler>()
        where TEvent : IDomainEvent
        where THandler : IDomainEventHandler<TEvent>
    {
        _registrations.Add(new TypedHandlerRegistration<TEvent, THandler>());
        return this;
    }

    /// <summary>
    /// Adds a new event handler registration to the internal list of registrations.
    /// </summary>
    /// <param name="registration">
    /// The event handler registration to be added. This registration specifies the type of
    /// event and its corresponding handler to be invoked during event dispatch.
    /// </param>
    internal void AddRegistration(IEventHandlerRegistration registration)
    {
        _registrations.Add(registration);
    }

    /// <summary>
    /// Retrieves all event handler registrations stored in the registry.
    /// </summary>
    /// <returns>A read-only list of event handler registrations.</returns>
    internal IReadOnlyList<IEventHandlerRegistration> GetRegistrations()
    {
        return _registrations;
    }
}
