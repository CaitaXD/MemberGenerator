using System;
using System.Collections.Generic;

namespace MemberGenerator;

public static class EqualityComparer
{
    public static IEqualityComparer<T> Create<T>(Func<T, T, bool> equals, Func<T, int>? getHashCode = null) =>
        new DelegateEqualityComparer<T>(equals, getHashCode);

    public static IEqualityComparer<T> Select<T, TResult>(Func<T, TResult> selector) =>
        new DelegateEqualityComparer<T>(
            (x, y) => selector(x)?.Equals(selector(y)) is true,
            x => selector(x)?.GetHashCode() ?? 0
        );
}

public class DelegateEqualityComparer<T>(Func<T, T, bool> equals, Func<T, int>? getHashCode = null)
    : IEqualityComparer<T>
{
    public bool Equals(T x, T y) =>
        equals(x, y);

    public int GetHashCode(T obj) =>
        getHashCode is null
            ? EqualityComparer<T>.Default.GetHashCode(obj)
            : getHashCode(obj);
}