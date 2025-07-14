using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using MemberGenerator.Ex;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator;

public record CSharpElement(ISymbol Symbol)
{
    public IReadOnlyList<SyntaxNode> Syntaxes { get; init; } = [];

    public string FileNameString(SymbolDisplayFormat? format = null)
    {
        var sb = new StringBuilder(Symbol.ToDisplayString(format));
        foreach (var c in Path.GetInvalidFileNameChars())
            sb.Replace(c, '_');
        sb.Append(".g.cs");
        return sb.ToString();
    }
}

public record CSharpMember(ISymbol Symbol) : CSharpElement(Symbol)
{
    CSharpSyntaxNode? _body;

    public new IReadOnlyList<MemberDeclarationSyntax> Syntaxes
    {
        get => base.Syntaxes.ToReadonlyList<SyntaxNode, MemberDeclarationSyntax>();
        init => base.Syntaxes = value;
    }

    public CSharpSyntaxNode? Body
    {
        get => _body ??= Syntaxes.Select(memberDeclarationSyntax => memberDeclarationSyntax switch
        {
            MethodDeclarationSyntax { Body: {} body } => body,
            MethodDeclarationSyntax { ExpressionBody: {} expressionBody } => expressionBody,
            PropertyDeclarationSyntax { ExpressionBody: {} expressionBody } => expressionBody,
            PropertyDeclarationSyntax { AccessorList: {} accessors } => accessors,
            IndexerDeclarationSyntax { ExpressionBody: {} expressionBody } => expressionBody,
            IndexerDeclarationSyntax { AccessorList: {} accessorList } => accessorList,
            _ => (CSharpSyntaxNode?)null,
        }).FirstOrDefault(cSharpSyntaxNode => cSharpSyntaxNode != null);
        init => _body = value;
    }
}

public record CSharpType(INamedTypeSymbol Symbol) : CSharpMember(Symbol)
{
    public new INamedTypeSymbol Symbol => (INamedTypeSymbol)base.Symbol;

    public new IReadOnlyList<TypeDeclarationSyntax> Syntaxes
    {
        get => base.Syntaxes.ToReadonlyList<SyntaxNode, TypeDeclarationSyntax>();
        init => base.Syntaxes = value;
    }

    ImmutableArray<CSharpMember>?    _members;
    ImmutableArray<CSharpInterface>? _interfaces;
    ImmutableArray<CSharpInterface>? _allInterfaces;
    ImmutableArray<TypeSyntax>?      _typeParameters;

    public ImmutableArray<CSharpInterface> AllInterfaces => _allInterfaces ??=
        Symbol.AllInterfaces.Select(i => new CSharpInterface(i)
        {
            Syntaxes = i.DeclaringSyntaxReferences
                .Select(x => x.GetSyntax())
                .OfType<InterfaceDeclarationSyntax>().ToReadonlyList()
        }).ToImmutableArray();

    public ImmutableArray<CSharpInterface> Interfaces => _interfaces ??=
        Symbol.Interfaces.Select(i => new CSharpInterface(i)
        {
            Syntaxes = i.DeclaringSyntaxReferences
                .Select(x => x.GetSyntax())
                .OfType<InterfaceDeclarationSyntax>().ToReadonlyList()
        }).ToImmutableArray();

    public ImmutableArray<TypeSyntax> TypeParametersTypeSyntaxes => _typeParameters ??=
        Syntaxes.SelectMany(x => x.TypeParameterList?.Parameters ?? [])
            .Select(x => SyntaxFactory.ParseTypeName(x.Identifier.Text))
            .ToImmutableArray();

    public ImmutableArray<CSharpMember> Members
    {
        get => _members ??= Symbol.GetMembers()
            .Select(m => new CSharpMember(m)
            {
                Syntaxes = m.DeclaringSyntaxReferences.Select(x => x.GetSyntax())
                    .OfType<MemberDeclarationSyntax>().ToReadonlyList()
            })
            .ToImmutableArray();
        init => _members = value;
    }
}

public record CSharpInterface(INamedTypeSymbol Symbol) : CSharpType(Symbol)
{
    public new INamedTypeSymbol Symbol => base.Symbol;

    public new IReadOnlyList<InterfaceDeclarationSyntax> Syntaxes
    {
        get => base.Syntaxes.ToReadonlyList<SyntaxNode, InterfaceDeclarationSyntax>();
        init => base.Syntaxes = value;
    }
}

public class CSharpEqualityComparer : IEqualityComparer<ISymbol>, IEqualityComparer<CSharpElement>
{
    static readonly SymbolDisplayFormat EqualityDisplayFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters
                       | SymbolDisplayMemberOptions.IncludeExplicitInterface,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType
    );
    public static CSharpEqualityComparer Instance { get; } = new();

    public bool Equals(ISymbol x, ISymbol y)
    {
        if (x.ToDisplayString(EqualityDisplayFormat) == y.ToDisplayString(EqualityDisplayFormat))
        {
            bool equals = true;
            if (x is INamedTypeSymbol xNamed && y is INamedTypeSymbol yNamed)
            {
                equals &= xNamed.TypeArguments.Length == yNamed.TypeArguments.Length;
            }
            else if (x is IMethodSymbol xMethod && y is IMethodSymbol yMethod)
            {
                equals &= xMethod.Parameters.Length == yMethod.Parameters.Length;
                equals &= xMethod.TypeArguments.Length == yMethod.TypeArguments.Length;
                equals &= xMethod.Parameters.Select(p => p.RefKind)
                    .SequenceEqual(yMethod.Parameters.Select(p => p.RefKind));
            }

            return equals;
        }

        return false;
    }

    public int GetHashCode(ISymbol obj)
    {
        int hash = obj.ToDisplayString(EqualityDisplayFormat).GetHashCode();
        if (obj is INamedTypeSymbol named)
            return CombineHashCode(ref hash, named.TypeArguments.Length);

        if (obj is IMethodSymbol method)
        {
            CombineHashCode(ref hash, method.Parameters.Length);
            CombineHashCode(ref hash, method.TypeArguments.Length);
            foreach (var parameter in method.Parameters)
                CombineHashCode(ref hash, ((byte)parameter.RefKind).GetHashCode());
            return hash;
        }

        if (obj is IPropertySymbol property)
        {
            CombineHashCode(ref hash, property.Parameters.Length);
            foreach (var parameter in property.Parameters)
                CombineHashCode(ref hash, parameter.RefKind);
            return hash;
        }

        return hash;
    }

    static int CombineHashCode<T>(ref int hash, T value)
    {
        unchecked
        {
            hash = (hash*397) ^ value?.GetHashCode() ?? 0;
        }

        return hash;
    }

    public bool Equals(CSharpElement x, CSharpElement y)
    {
        return Equals(x.Symbol, y.Symbol);
    }

    public int GetHashCode(CSharpElement obj)
    {
        return GetHashCode(obj.Symbol);
    }
}