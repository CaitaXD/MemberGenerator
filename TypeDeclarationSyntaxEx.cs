using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator;

public static class TypeDeclarationSyntaxEx
{
    public static TypeDeclarationSyntax CreatePartialType(this TypeDeclarationSyntax syntax)
    {
        var trailingTrivia = syntax.GetTrailingTrivia();

        syntax = syntax.WithBaseList(null);

        var lineEnd = SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed);
        if (syntax.OpenBraceToken.IsKind(SyntaxKind.None))
        {
            var openBraceToken = SyntaxFactory.Token(lineEnd, SyntaxKind.OpenBraceToken, lineEnd);
            syntax = syntax.WithOpenBraceToken(openBraceToken);
        }

        if (!syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            var whitespace = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
            var partialKeyword = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.PartialKeyword, whitespace);
            syntax = syntax.WithModifiers(syntax.Modifiers.Add(partialKeyword));
        }

        if (syntax is RecordDeclarationSyntax { ParameterList: not null } recordSyntax)
        {
            syntax = recordSyntax.WithParameterList(null);
        }

        if (syntax.CloseBraceToken.IsKind(SyntaxKind.None))
        {
            var closeBraceToken = SyntaxFactory.Token(lineEnd, SyntaxKind.CloseBraceToken, SyntaxTriviaList.Empty);
            syntax = syntax.WithCloseBraceToken(closeBraceToken);
        }

        return syntax.NormalizeWhitespace().WithTrailingTrivia(trailingTrivia);
    }

    public static bool IsDeclarationOf(this TypeDeclarationSyntax syntax, BaseTypeSyntax baseTypeSyntax)
    {
        var typeParameterList = syntax.TypeParameterList;
        if (!syntax.Identifier.IsEquivalentTo(baseTypeSyntax.GetFirstToken()))
        {
            // TODO: This is highly suspicious
            if (syntax.Identifier.Text == baseTypeSyntax.ToString())
            {
                return true;
            }
            return false;
        }

        if (baseTypeSyntax.FirstChildNoteOrDefault<GenericNameSyntax>() is not {} genericNameSyntax)
            return false;

        var genericParameterList = genericNameSyntax.TypeArgumentList;
        return typeParameterList?.Parameters.Count == genericParameterList.Arguments.Count;
    }

    public static TypeDeclarationSyntax WithInterfaceMembers(this TypeDeclarationSyntax syntax,
        params IEnumerable<MemberDeclarationSyntax> members) =>
        syntax switch
        {
            InterfaceDeclarationSyntax => syntax.WithMembers(
                SyntaxFactory.List(members
                    .AddModifierIfNotExists(SyntaxFactory.Token(SyntaxKind.NewKeyword))
                )
            ),
            _ => syntax.WithMembers(SyntaxFactory.List(members.AddModifierIfNotExists(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword)
            ))),
        };


    [Obsolete("Now that i think about it, i dont see why would i want this")]
    public static TypeDeclarationSyntax WithExplicitInterfaceMembers(
        this TypeDeclarationSyntax syntax,
        BaseTypeSyntax interfaceSyntax,
        params IEnumerable<MemberDeclarationSyntax> members)
    {
        var syntaxList = new SyntaxList<MemberDeclarationSyntax>();
        foreach (var memberSyntax in members)
            syntaxList = syntaxList.Add(memberSyntax.ToExplicit(syntax, interfaceSyntax));
        return syntax.WithMembers(syntaxList);
    }

    public static string GetNamespace(this BaseTypeDeclarationSyntax syntax)
    {
        string nameSpace = string.Empty;

        SyntaxNode? potentialParent = syntax.Parent;

        while (potentialParent is not null
               and not NamespaceDeclarationSyntax
               and not FileScopedNamespaceDeclarationSyntax)
        {
            potentialParent = potentialParent.Parent;
        }

        if (potentialParent is BaseNamespaceDeclarationSyntax namespaceParent)
        {
            nameSpace = namespaceParent.Name.ToString();
            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent) break;
                nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                namespaceParent = parent;
            }
        }

        return nameSpace;
    }
}