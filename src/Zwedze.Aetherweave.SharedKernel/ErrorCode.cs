using JetBrains.Annotations;

namespace Zwedze.Aetherweave.SharedKernel;

public readonly struct ErrorCode : IEquatable<ErrorCode>
{
    private readonly string _value;
    public static readonly ErrorCode UnmanagedErrorCode = New("__unmanaged_error");

    [UsedImplicitly]
    public static ErrorCode New(string value)
    {
        return new ErrorCode(value);
    }

    private ErrorCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _value = value;
    }

    public static implicit operator string(ErrorCode id)
    {
        return id._value;
    }

    public static explicit operator ErrorCode(string value)
    {
        return new ErrorCode(value);
    }

    public static bool operator ==(ErrorCode left, ErrorCode right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ErrorCode left, ErrorCode right)
    {
        return !Equals(left, right);
    }

    public bool Equals(ErrorCode other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ErrorCode other && Equals(other);
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
