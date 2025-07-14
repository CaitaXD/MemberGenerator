using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MemberGenerator.Ex;

public static class LinqEx
{
    public static IReadOnlyList<T> ToReadonlyList<T>(this IEnumerable<T> source)
    {
        if (source is IReadOnlyList<T> list)
            return list;
        return source.ToArray();
    }

    public static IReadOnlyList<TResult> ToReadonlyList<T, TResult>(this IReadOnlyList<T> source)
    {
        if (source is IReadOnlyList<TResult> list)
            return list;
        return source.OfType<TResult>().ToArray();
    }

    public static string JoinToString<T>(this IEnumerable<T> source, string separator = ", ") =>
        string.Join(separator, source.Select(x => x?.ToString() ?? "<null>"));
}