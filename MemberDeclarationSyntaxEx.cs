using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator;

public static class MemberDeclarationSyntaxEx
{
    public static MemberDeclarationSyntax WithoutModifier(this MemberDeclarationSyntax syntax, SyntaxToken token) =>
        syntax.WithModifiers(syntax.Modifiers.Remove(token));

    public static MemberDeclarationSyntax WithModifier(this MemberDeclarationSyntax syntax, SyntaxToken token) =>
        syntax.WithModifiers(syntax.Modifiers.Add(token));

    public static IEnumerable<MemberDeclarationSyntax> WithModifier(
        this IEnumerable<MemberDeclarationSyntax> members,
        SyntaxToken token)
    {
        foreach (var t in members)
            yield return t.WithModifiers(t.Modifiers.Add(token));
    }

    public static MemberSignature GetSignature(this MemberDeclarationSyntax syntax) =>
        MemberSignature.FromDeclarationSyntax(syntax);

    public static MemberDeclarationSyntax ToExplicit(
        this MemberDeclarationSyntax memberSyntax,
        TypeDeclarationSyntax typeDeclarationSyntax,
        BaseTypeSyntax baseTypeSyntax)
    {
        if (typeDeclarationSyntax is InterfaceDeclarationSyntax)
        {
            memberSyntax = memberSyntax.WithModifiers(
                memberSyntax.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.NewKeyword))
            );
            return memberSyntax;
        }

        if (memberSyntax.FirstChildTokenOrDefault(SyntaxKind.PublicKeyword) is not {} publicKeyword)
            return memberSyntax;

        var genericNameSyntax = baseTypeSyntax.FirstChildNode<GenericNameSyntax>();
        var separatedTypeSyntaxList = genericNameSyntax?.TypeArgumentList ?? SyntaxFactory.TypeArgumentList();

        var typeArgumentTokens = Enumerable.Empty<SyntaxToken>();
        if (separatedTypeSyntaxList.Arguments.Count > 0)
            typeArgumentTokens = separatedTypeSyntaxList.TraverseTokens();

        memberSyntax = memberSyntax.WithoutModifier(publicKeyword);

        var memberNameToken = memberSyntax.LastChildToken(SyntaxKind.IdentifierToken);
        var typeNameToken = baseTypeSyntax.GetFirstToken();

        var syntaxTokenList = SyntaxFactory.TokenList([
                typeNameToken,
                ..typeArgumentTokens,
                SyntaxFactory.Token(SyntaxKind.DotToken),
                memberNameToken
            ]
        );

        if (memberSyntax is MethodDeclarationSyntax methodSyntax)
        {
            methodSyntax = methodSyntax.WithIdentifier(SyntaxFactory.Identifier(
                syntaxTokenList.ToString()
            ));

            methodSyntax = methodSyntax.WithConstraintClauses([]);
            memberSyntax = methodSyntax;
        }

        return memberSyntax.NormalizeWhitespace();
    }
}