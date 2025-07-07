using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MemberGenerator.Ex;

public static class ConsoleEx
{
    public static void JoinLine<T>(string separator, IEnumerable<T> strings, string prefix = "", string suffix = "") =>
        Console.WriteLine(
            prefix +
            string.Join(separator, strings.Select(x => x?.ToString() ?? "<null>"))
            + suffix
        );
    
    public static void JoinLine<T>(string separator, IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableArray<T>> source, string prefix = "", string suffix = "") =>
        context.RegisterSourceOutput(source, (_, state) =>
            JoinLine(separator, state.Select(x => x?.ToString()), prefix, suffix)
        );
}