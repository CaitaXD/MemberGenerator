using System.Collections.Generic;
using System.Collections.Immutable;

namespace MemberGenerator.Ex;

public static class LinqEx
{
    public static IReadOnlyCollection<T> ToReadonlyCollection<T>(this IEnumerable<T> source)
    {
        if (source is IReadOnlyCollection<T> collection)
            return collection;
        return source.ToImmutableArray();
    }
}