using System;

namespace MemberGenerator;

public static class Functional
{
    
}

public static class Thunk
{
    public static T Invoke<T>(Func<T> func) => func();
    public static T Invoke<T,TState>(Func<TState, T> func, TState state) => func(state);
}