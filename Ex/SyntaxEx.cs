using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator.Ex;

public static class SyntaxEx
{
    public static TSyntax? FirstChildNodeOrDefault<TSyntax>(this SyntaxNode syntaxNode)
        where TSyntax : SyntaxNode =>
        syntaxNode.ChildNodes().OfType<TSyntax>().FirstOrDefault();

    public static TSyntax? LastChildNodeOrDefault<TSyntax>(this SyntaxNode syntaxNode)
        where TSyntax : SyntaxNode =>
        syntaxNode.ChildNodes().OfType<TSyntax>().LastOrDefault();

    public static TSyntax FirstChildNode<TSyntax>(this SyntaxNode syntaxNode)
        where TSyntax : SyntaxNode =>
        syntaxNode.ChildNodes().OfType<TSyntax>().First();

    public static TSyntax LastChildNode<TSyntax>(this SyntaxNode syntaxNode)
        where TSyntax : SyntaxNode =>
        syntaxNode.ChildNodes().OfType<TSyntax>().Last();

    public static SyntaxToken? FirstChildTokenOrDefault(this SyntaxNode syntaxNode, SyntaxKind kind)
    {
        foreach (SyntaxToken child in syntaxNode.ChildTokens())
            if (child.IsKind(kind))
                return child;
        return null;
    }

    public static SyntaxToken? LastChildTokenOrDefault(this SyntaxNode syntaxNode, SyntaxKind kind)
    {
        foreach (SyntaxToken child in syntaxNode.ChildTokens().Reverse())
            if (child.IsKind(kind))
                return child;
        return null;
    }

    public static SyntaxToken FirstChildToken(this SyntaxNode syntaxNode, SyntaxKind kind) =>
        syntaxNode.FirstChildTokenOrDefault(kind)!.Value;

    public static SyntaxToken LastChildToken(this SyntaxNode syntaxNode, SyntaxKind kind) =>
        syntaxNode.LastChildTokenOrDefault(kind)!.Value;

    public static void Visit(this SyntaxNode syntax, Action<SyntaxNode> accept)
    {
        HashSet<SyntaxNode> visited = [];
        Queue<SyntaxNode> queue = new();
        queue.Enqueue(syntax);

        while (queue.Count > 0)
        {
            SyntaxNode? node = queue.Dequeue();
            if (!visited.Add(node)) continue;
            accept(node);
            foreach (SyntaxNode? childNode in node.ChildNodes())
                queue.Enqueue(childNode);
        }
    }


    public static ImmutableArray<SyntaxToken> TraverseTokens(this SyntaxNode syntax)
    {
        var stack = new Stack<SyntaxNodeOrToken>();
        ImmutableArray<SyntaxToken>.Builder? builder = ImmutableArray.CreateBuilder<SyntaxToken>();
        stack.Push(syntax);
        while (stack.Count > 0)
        {
            SyntaxNodeOrToken current = stack.Pop();

            if (current.IsToken)
            {
                builder.Insert(0, current.AsToken());
                continue;
            }

            foreach (SyntaxNodeOrToken child in current.ChildNodesAndTokens())
                stack.Push(child);
        }

        return builder.ToImmutable();
    }

    public static bool IsPartialTypeDeclaration(this SyntaxNode syntax) =>
        syntax is TypeDeclarationSyntax { Modifiers: { Count: > 0 } modifiers } &&
        modifiers.Any(SyntaxKind.PartialKeyword);
}