using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator.Ex;

public static class SymbolEx
{
    public static IEnumerable<INamespaceSymbol> GetAllNamespaces(this INamespaceSymbol root)
    {
        yield return root;
        foreach (var child in root.GetNamespaceMembers())
        foreach (var next in GetAllNamespaces(child))
            yield return next;
    }

    public static IEnumerable<INamedTypeSymbol> AllNestedTypesAndSelf(this INamedTypeSymbol type)
    {
        yield return type;
        foreach (var typeMember in type.GetTypeMembers())
        {
            foreach (var nestedType in typeMember.AllNestedTypesAndSelf())
            {
                yield return nestedType;
            }
        }
    }

    static IImmutableDictionary<IAssemblySymbol, IImmutableSet<INamedTypeSymbol>> _assemblyCache =
        ImmutableDictionary<IAssemblySymbol, IImmutableSet<INamedTypeSymbol>>.Empty;

    static IImmutableDictionary<SyntaxNode, IImmutableSet<INamedTypeSymbol>> _syntaxCache =
        ImmutableDictionary<SyntaxNode, IImmutableSet<INamedTypeSymbol>>.Empty;

    public static IImmutableSet<INamedTypeSymbol> GetAllTypeSymbols(this IAssemblySymbol assembly)
    {
        if (_assemblyCache.TryGetValue(assembly, out var types)) return types;
        var fresh = GetImplementationsFrom(assembly);
        _assemblyCache = _assemblyCache.Add(assembly, fresh);
        return fresh;
    }

    static IImmutableSet<INamedTypeSymbol> GetImplementationsFrom(IAssemblySymbol assemblySymbol) =>
        GetAllNamespaces(assemblySymbol.GlobalNamespace)
            .SelectMany(ns => ns.GetTypeMembers())
            .SelectMany(t => t.AllNestedTypesAndSelf())
            .Where(TypeFilter)
            .ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

    static bool TypeFilter(INamedTypeSymbol nts) =>
        nts is
        {
            IsStatic: false, IsImplicitClass: false, IsScriptClass: false,
            TypeKind: TypeKind.Class or TypeKind.Struct or TypeKind.Interface,
            DeclaredAccessibility: Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal
        };

    public static void CollectAssemblySymbols(this IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider
            .SelectMany(static (ctx, _) => ctx.Assembly.GetAllTypeSymbols())
            .Collect(), (_, _) => {});
    }
}