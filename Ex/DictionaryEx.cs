using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MemberGenerator.Ex;

public static class DictionaryEx
{
    public static ImmutableDictionary<TKey, TValue> Combine<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue> first,
        IEnumerable<KeyValuePair<TKey, TValue>> second) where TKey : notnull
    {
        var result = first.ToBuilder();
        foreach (var kvp in second) 
            result[kvp.Key] = kvp.Value;

        return result.ToImmutable();
    }

    public static ImmutableDictionary<TKey, TValue> Combine<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue> first,
        IEnumerable<(TKey Key, TValue Value)> second) where TKey : notnull
    {
        var result = first.ToBuilder();
        foreach ((TKey Key, TValue Value) kvp in second)
        {
            result[kvp.Key] = kvp.Value;
        }

        return result.ToImmutable();
    }

    public static ImmutableDictionary<TKey, IEnumerable<TValue>> GroupByToImmutableDictionary<TValue, TKey>(
        this IEnumerable<TValue> source,
        Func<TValue, TKey> keySelector) where TKey : notnull =>
        source.GroupBy(keySelector).ToImmutableDictionary(g => g.Key, g => g.AsEnumerable());
    
    public static ImmutableDictionary<TKey, IEnumerable<TResult>> GroupByToImmutableDictionary<TKey, TValue, TResult>(
        this IEnumerable<TValue> source,
        Func<TValue, TKey> keySelector,
        Func<TValue, TResult> elementSelector) where TKey : notnull =>
        source.GroupBy(keySelector).ToImmutableDictionary(g => g.Key, g => g.Select(elementSelector));
}