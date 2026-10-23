using JetBrains.Annotations;

namespace Zwedze.Aetherweave.SharedKernel;

public readonly struct ErrorCode : IEquatable<ErrorCode>
{
    public string Value { get; }

    public static readonly ErrorCode UnmanagedErrorCode = New("__unmanaged_error");

    [UsedImplicitly]
    public static ErrorCode New(string value)
    {
        return new ErrorCode(value);
    }

    private ErrorCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static implicit operator string(ErrorCode id)
    {
        return id.Value;
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
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ErrorCode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }
}
