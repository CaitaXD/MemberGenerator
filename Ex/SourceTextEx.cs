global using static MemberGenerator.Ex.SourceTextEx;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace MemberGenerator.Ex;

public static class SourceTextEx
{
    public static string FormatedChecksum(SourceText sourceText, int? take = null)
    {
        var sb = new StringBuilder();
        foreach (var b in sourceText.GetChecksum())
            sb.Append(b.ToString("X2"));

        if (take.HasValue) sb.Length = take.Value;
        return sb.ToString();
    }
}