using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zwedze.Aetherweave.Application.DomainEventHandlers;
using Zwedze.Aetherweave.SharedKernel.DomainEvents;
using IDomainEventDispatcher = Zwedze.Aetherweave.Application.DomainEventHandlers.IDomainEventDispatcher;

namespace Zwedze.Aetherweave.Application.Extensions;

public static class DomainEventDispatcherExtensions
{
    public static IServiceCollection AddDomainEventDispatcher(this IServiceCollection services, Action<DomainEventHandlerRegistry> configure)
    {
        var registry = new DomainEventHandlerRegistry();
        configure(registry);

        services.AddScoped<IDomainEventDispatcher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<DomainEventDispatcher>>();
            var dispatcher = new DomainEventDispatcher(logger);

            // Bind registrations to DI at resolution time
            foreach (var registration in registry.GetRegistrations())
            {
                registration.Register(dispatcher, sp);
            }

            return dispatcher;
        });

        return services;
    }
}

internal sealed class TypedHandlerRegistration<TEvent, THandler> : IEventHandlerRegistration
    where TEvent : IDomainEvent
    where THandler : IDomainEventHandler<TEvent>
{
    public void Register(DomainEventDispatcher dispatcher, IServiceProvider serviceProvider)
    {
        dispatcher.AddHandler(() => serviceProvider.GetRequiredService<THandler>());
    }
}

public sealed class EventHandlerConfigurator<TEvent> where TEvent : IDomainEvent
{
    private readonly DomainEventHandlerRegistry _registry;

    internal EventHandlerConfigurator(DomainEventHandlerRegistry registry)
    {
        _registry = registry;
    }

    public EventHandlerConfigurator<TEvent> AddHandler<THandler>()
        where THandler : IDomainEventHandler<TEvent>
    {
        _registry.AddRegistration(new TypedHandlerRegistration<TEvent, THandler>());
        return this;
    }

    public EventHandlerConfigurator<TNextEvent> Configure<TNextEvent>()
        where TNextEvent : IDomainEvent
    {
        return _registry.Configure<TNextEvent>();
    }
}

internal interface IEventHandlerRegistration
{
    void Register(DomainEventDispatcher dispatcher, IServiceProvider serviceProvider);
}
