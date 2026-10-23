namespace Zwedze.Aetherweave.SharedKernel;

public static class CodeExtensions
{
    public static Code<T> ToCode<T>(this string value)
    {
        return Code<T>.From(value);
    }
}
