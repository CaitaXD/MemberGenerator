using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator.Ex;

public static class TypesEx
{
    public static bool IsPartial(this TypeDeclarationSyntax syntax) =>
        syntax is { Modifiers: { Count: > 0 } modifiers } && modifiers.Any(SyntaxKind.PartialKeyword);

    public static bool HasBaseList(this TypeDeclarationSyntax syntax) =>
        syntax.BaseList is { Types.Count: > 0 };

    public static CSharpType WithTypesQualified(this CSharpType cSharpType)
    {
        var displayFormat = SymbolDisplayEx.IdentifierDisplayFormat.With(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            genericsOptions: SymbolDisplayEx.IdentifierDisplayFormat.GenericsOptions
                             & ~SymbolDisplayGenericsOptions.IncludeTypeConstraints
        );

        var memberDeclarations = cSharpType.Members
            .SyntaxDeclarations()
            .OfType<TypeDeclarationSyntax>()
            .Select(x => x.Identifier.Text);

        var newMembers = cSharpType.Members.Select(m => m with
            {
                Body = (CSharpSyntaxNode?)m.Body?.Visit(node =>
                {
                    if (node is IdentifierNameSyntax syntax)
                    {
                        if (memberDeclarations.Contains(syntax.Identifier.Text))
                        {
                            var newIdentifier = SyntaxFactory.Identifier(
                                $"{cSharpType.Symbol.ToDisplayString(displayFormat)}.{syntax.Identifier.Text}"
                            );
                            return syntax.WithIdentifier(newIdentifier);
                        }
                    }

                    return node;
                })
            }
        );

        return cSharpType with { Members = newMembers.ToImmutableArray() };
    }
}