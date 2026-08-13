namespace Zwedze.Aetherweave.SharedKernel;

public static class IdExtensions
{
    public static IReadOnlyCollection<long> ToLong<T>(this IEnumerable<Id<T>> ids)
    {
        return [.. ids.Select(x => (long)x)];
    }
}
