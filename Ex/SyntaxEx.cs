using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MemberGenerator.Ex;

public static class SyntaxEx
{
    public static SyntaxNode Visit(this SyntaxNode syntax, Func<SyntaxNode?, SyntaxNode?> accept)
    {
        var visitor = new DelegateRewriter(accept);
        var clone = SyntaxFactory.SyntaxTree(syntax).GetRoot();
        return visitor.Visit(clone)!;
    }
}

public sealed class DelegateRewriter(Func<SyntaxNode?, SyntaxNode?> func) : CSharpSyntaxRewriter
{
    public override SyntaxNode? Visit(SyntaxNode? node) =>
        base.Visit(func(node));
}