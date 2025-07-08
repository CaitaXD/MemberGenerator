using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator.Ex;

public static class SymbolEx
{
    public static bool ImplementsDirectly(this INamedTypeSymbol namedSymbol, string interfaceName)
    {
        if (!namedSymbol.IsGenericType) return namedSymbol.Name == interfaceName;

        var interfaceNameSpan = interfaceName.AsSpan();
        int indexOfAngleBracket = interfaceNameSpan.IndexOf('<');
        if (indexOfAngleBracket == -1) return false;

        var interfaceNameWithoutTypeParameters = interfaceNameSpan.Slice(0, indexOfAngleBracket);

        foreach (INamedTypeSymbol? baseType in namedSymbol.Interfaces)
        {
            var baseTypeName = baseType.Name.AsSpan();
            bool sameName = baseTypeName.SequenceEqual(interfaceNameWithoutTypeParameters);
            bool sameArity = baseType.Arity == TypeNameArity(interfaceNameSpan);
            if (sameName && sameArity)
                return true;
        }

        return false;
    }

    public static bool Implements(this INamedTypeSymbol namedSymbol, string interfaceName)
    {
        if (!namedSymbol.IsGenericType) return namedSymbol.Name == interfaceName;

        var interfaceNameSpan = interfaceName.AsSpan();
        int indexOfAngleBracket = interfaceNameSpan.IndexOf('<');
        if (indexOfAngleBracket == -1) return false;

        var interfaceNameWithoutTypeParameters = interfaceNameSpan.Slice(0, indexOfAngleBracket);

        foreach (INamedTypeSymbol? baseType in namedSymbol.AllInterfaces)
        {
            var baseTypeName = baseType.Name.AsSpan();
            bool sameName = baseTypeName.SequenceEqual(interfaceNameWithoutTypeParameters);
            bool sameArity = baseType.Arity == TypeNameArity(interfaceNameSpan);
            if (sameName && sameArity)
                return true;
        }

        return false;
    }

    public static int TypeNameArity(ReadOnlySpan<char> typeName)
    {
        int arity = 0;
        foreach (var c in typeName)
        {
            if (c is '<' or ',')
                arity++;
        }

        return arity;
    }
}