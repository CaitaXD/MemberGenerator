using Microsoft.CodeAnalysis;

namespace MemberGenerator;

public static class SyntaxProviderEx
{
    public static IncrementalValuesProvider<T> CreateSyntaxProvider<T>(this SyntaxValueProvider syntaxProvider)
        where T : SyntaxNode =>
        syntaxProvider.CreateSyntaxProvider(
            static (syntax, _) => syntax is T,
            static (context, _) => (T)context.Node
        );

    public static IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(this SyntaxValueProvider syntaxProvider,
        string attributeName)
        where T : SyntaxNode =>
        syntaxProvider.ForAttributeWithMetadataName(attributeName,
            static (syntax, _) => syntax is T,
            static (context, _) => (T)context.TargetNode
        );
}