using JetBrains.Annotations;

namespace Zwedze.Aetherweave.SharedKernel;

public readonly struct Id<T> : IEquatable<Id<T>>
{
    private readonly long _value;

    [UsedImplicitly]
    public static Id<T> From(long value)
    {
        return new Id<T>(value);
    }

    private Id(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        _value = value;
    }

    public static explicit operator long(Id<T> id)
    {
        return id._value;
    }

    public static explicit operator Id<T>(long value)
    {
        return new Id<T>(value);
    }

    public static bool operator ==(Id<T> left, Id<T> right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Id<T> left, Id<T> right)
    {
        return !Equals(left, right);
    }

    public bool Equals(Id<T> other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Id<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override string ToString()
    {
        return _value.ToString();
    }
}
