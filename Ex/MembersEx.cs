using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator.Ex;

public static class MembersEx
{
    public static bool HasBodyOrExpressionBody(this MemberDeclarationSyntax memberSyntax) =>
        memberSyntax switch
        {
            MethodDeclarationSyntax { Body: not null } => true,
            MethodDeclarationSyntax { ExpressionBody: not null } => true,
            PropertyDeclarationSyntax { ExpressionBody: not null } => true,
            PropertyDeclarationSyntax { AccessorList: not null } => true,
            IndexerDeclarationSyntax { ExpressionBody: not null } => true,
            IndexerDeclarationSyntax { AccessorList: not null } => true,
            _ => false,
        };

    public static IEnumerable<CSharpMember> WithSyntaxWhere(this ImmutableArray<CSharpMember> members,
        Func<MemberDeclarationSyntax, bool> predicate) =>
        members.Select(m => m with
        {
            Syntaxes = m.Syntaxes.Where(predicate).ToReadonlyList()
        });

    public static IEnumerable<MemberDeclarationSyntax> SyntaxDeclarations(this IEnumerable<CSharpMember> members) =>
        members.SelectMany(m => m.Syntaxes);

    public static CSharpMember GenericSubstitution(this CSharpMember cSharpMember,
        Dictionary<string, string> typeIdentifierMap)
    {
        var visitor = new GenericSubstitutionRewriter(typeIdentifierMap);
        if (cSharpMember.Body is null) return cSharpMember;

        var clone = cSharpMember.Body?.SyntaxTree.GetRoot();
        var newBody = visitor.Visit(clone);
        return cSharpMember with
        {
            Body = (CSharpSyntaxNode?)newBody
        };
    }

    public static string AccessibilityString(this Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedAndInternal => "protected internal",
            Accessibility.NotApplicable => "",
            Accessibility.ProtectedOrInternal => "protected internal",
            _ => throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null)
        };
}

internal class GenericSubstitutionRewriter(Dictionary<string, string> typeIdentifierMap)
    : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (typeIdentifierMap.TryGetValue(node.Identifier.Text, out var output))
            return base.VisitIdentifierName(SyntaxFactory.IdentifierName(output));
        return base.VisitIdentifierName(node);
    }

    // public override SyntaxNode? VisitCastExpression(CastExpressionSyntax node)
    // {
    //     if (typeIdentifierMap.TryGetValue(node.Type.ToString(), out var output))
    //         return base.VisitCastExpression(node.WithType(SyntaxFactory.ParseTypeName(output)));
    //     return base.VisitCastExpression(node);
    // }
}