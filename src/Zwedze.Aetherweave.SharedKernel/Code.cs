using JetBrains.Annotations;

namespace Zwedze.Aetherweave.SharedKernel;

public readonly struct Code<T> : IEquatable<Code<T>>
{
    private readonly string _value;

    [UsedImplicitly]
    public static Code<T> From(string value)
    {
        return new Code<T>(value);
    }

    private Code(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _value = value;
    }

    public static explicit operator string(Code<T> id)
    {
        return id._value;
    }

    public static explicit operator Code<T>(string value)
    {
        return new Code<T>(value);
    }

    public static bool operator ==(Code<T> left, Code<T> right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Code<T> left, Code<T> right)
    {
        return !Equals(left, right);
    }

    public bool Equals(Code<T> other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Code<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override string ToString()
    {
        return _value;
    }
}
