namespace Zwedze.Aetherweave.SharedKernel;

public static class IdExtensions
{
    public static Id<T> ToId<T>(this long value)
    {
        return Id<T>.From(value);
    }
}
