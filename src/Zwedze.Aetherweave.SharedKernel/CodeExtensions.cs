namespace Zwedze.Aetherweave.SharedKernel;

public static class CodeExtensions
{
    public static IReadOnlyCollection<string> ToLong<T>(this IEnumerable<Code<T>> codes)
    {
        return [.. codes.Select(x => (string)x)];
    }
}
