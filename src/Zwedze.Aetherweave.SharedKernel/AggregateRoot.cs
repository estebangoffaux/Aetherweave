using System.Collections.Immutable;
using JetBrains.Annotations;
using Zwedze.Aetherweave.SharedKernel.DomainEvents;

namespace Zwedze.Aetherweave.SharedKernel;

public interface IAggregateRoot<TEntity> : IHasDomainEvents
{
    Id<TEntity> Id { get; }
    Code<TEntity> Code { get; }
}

public abstract class AggregateRoot<TEntity>(Id<TEntity> id, Code<TEntity> code) : IAggregateRoot<TEntity>
{
    private readonly List<IDomainEvent> _events = new();

    public Id<TEntity> Id { get; } = id;
    public Code<TEntity> Code { get; } = code;

    public ImmutableArray<IDomainEvent> PopDomainEvents()
    {
        var events = _events.ToImmutableArray();
        _events.Clear();
        return events;
    }

    private bool Equals(AggregateRoot<TEntity> other)
    {
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((AggregateRoot<TEntity>)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    [UsedImplicitly]
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _events.Add(domainEvent);
    }

    public static bool operator !=(AggregateRoot<TEntity>? left, AggregateRoot<TEntity>? right)
    {
        return !(left == right);
    }

    public static bool operator ==(AggregateRoot<TEntity>? left, AggregateRoot<TEntity>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Id == right.Id;
    }
}
