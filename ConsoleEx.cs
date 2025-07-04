using System;
using System.Collections.Generic;
using System.Linq;

namespace MemberGenerator;

public static class ConsoleEx
{
    public static void JoinLine<T>(string separator, IEnumerable<T> strings, string prefix = "", string suffix = "") =>
        Console.WriteLine(
            prefix +
            string.Join(separator, strings.Select(x => x?.ToString() ?? "<null>"))
            + suffix
        );
}