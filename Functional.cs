using System;

namespace MemberGenerator;

public static class Functional
{
    public static Func<T1, T3> Compose<T1, T2, T3>(this Func<T1, T2> f, Func<T2, T3> g) =>
        x => g(f(x));
    
    public static Func<T1, T3> Pipe<T1, T2, T3>(this Func<T2, T3> f, Func<T1, T2> g) =>
        x => f(g(x));
}

public static class Thunk
{
    public static T Invoke<T>(Func<T> func) => func();
    public static T Invoke<T,TState>(Func<TState, T> func, TState state) => func(state);
}