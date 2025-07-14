global using static MemberGenerator.Ex.SbEx;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator.Ex;

public static class SbEx
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToStringAndClear(this StringBuilder sb)
    {
        var result = sb.ToString();
        sb.Clear();
        return result;
    }

    public static void AppendParentNamespace(this StringBuilder sb, ISymbol symbol)
    {
        if (!symbol.ContainingNamespace.IsGlobalNamespace)
            sb.AppendLine($"namespace {symbol.ContainingNamespace};");
    }

    public static void AppendTypeIdentifierName(this StringBuilder sb, ISymbol symbol)
    {
        var typeSyntaxes = symbol.DeclaringSyntaxReferences.Select(x => x.GetSyntax())
            .OfType<TypeDeclarationSyntax>().ToReadonlyList();

        var accessModifiers = typeSyntaxes
            .SelectMany(l => l.Modifiers.Select(t => t.ToString()))
            .Distinct()
            .JoinToString(" ");

        sb.Append(accessModifiers);
        sb.Append(" ");

        if (typeSyntaxes.Any(t => t is RecordDeclarationSyntax))
        {
            sb.Append("record ");
        }

        if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Class })
        {
            sb.Append(" class ");
        }
        else if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Struct })
        {
            sb.Append(" struct ");
        }
        else if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Interface })
        {
            sb.Append(" interface ");
        }
        else if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Delegate })
        {
            sb.Append(" delegate ");
        }
        else if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Enum })
        {
            sb.Append(" enum ");
        }
        else
        {
            sb.Append(" ");
        }

        var parts = symbol.ToDisplayParts(SymbolDisplayEx.IdentifierDisplayFormat);

        foreach (var part in parts.SkipWhile(part => part.Symbol?.Name != symbol.Name))
            sb.Append(part);
    }

    public static void AppendParentTypesBegin(this StringBuilder sb, ISymbol symbol)
    {
        var parentSymbol = symbol.ContainingType;
        while (parentSymbol is not null)
        {
            AppendTypeIdentifierName(sb, parentSymbol);
            sb.AppendLine("{");
            parentSymbol = parentSymbol.ContainingType;
        }
    }

    public static void AppendParentTypesEnd(this StringBuilder sb, ISymbol symbol)
    {
        var parentSymbol = symbol.ContainingType;
        while (parentSymbol is not null)
        {
            sb.AppendLine("}");
            parentSymbol = parentSymbol.ContainingType;
        }
    }


    static readonly SymbolDisplayFormat ParameterDisplayFormat = SymbolDisplayEx.IdentifierDisplayFormat.With(
        genericsOptions: SymbolDisplayEx.IdentifierDisplayFormat.GenericsOptions &
                         ~ SymbolDisplayGenericsOptions.IncludeTypeConstraints
    );

    public static void AppendParameters(this StringBuilder sb, ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
        {
            sb.Append("(");
            sb.Append(string.Join(", ",
                method.Parameters.Select(p => p.Type.ToDisplayString(ParameterDisplayFormat))));
            sb.Append(")");
        }

        if (symbol is IPropertySymbol { IsIndexer: true } property)
        {
            sb.Append("[");
            sb.Append(string.Join(", ",
                property.Parameters.Select(p => p.Type.ToDisplayString(ParameterDisplayFormat))));
            sb.Append("]");
        }
    }

    public static void AppendReturnType(this StringBuilder sb, ISymbol symbol)
    {
        if (symbol is IMethodSymbol { ReturnType: {} returnType })
        {
            sb.Append(returnType.ToDisplayString(ParameterDisplayFormat));
            sb.Append(" ");
        }
        else if (symbol is IPropertySymbol { Type: {} type })
        {
            sb.Append(type.ToDisplayString(ParameterDisplayFormat));
            sb.Append(" ");
        }
    }
}