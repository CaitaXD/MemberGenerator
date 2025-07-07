using MemberGenerator.Ex;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator.Factory;

public static class TypeDeclarationSyntaxFactory
{
    public static TypeDeclarationSyntax CreatePartialType(TypeDeclarationSyntax syntax)
    {
        var trailingTrivia = syntax.GetTrailingTrivia();

        syntax = syntax.WithBaseList(null);

        if (syntax.FirstChildNodeOrDefault<ParameterListSyntax>() is {} parameterListSyntax)
        {
            if (syntax.RemoveNode(parameterListSyntax, SyntaxRemoveOptions.KeepNoTrivia) is {} newSyntax)
                syntax = newSyntax;
        }

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
}