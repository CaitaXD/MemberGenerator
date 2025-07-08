using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator;

public readonly struct MemberSignature : IEquatable<MemberSignature>
{
    public SyntaxToken? Identifier { get; init; }
    public BaseParameterListSyntax? Parameters { get; init; }
    public VariableDeclarationSyntax? Declaration { get; init; }
    public ExplicitInterfaceSpecifierSyntax? ExplicitInterfaceSpecifier { get; init; }

    public MemberSignature(SyntaxToken identifier, BaseParameterListSyntax? parameters)
    {
        Identifier = identifier.WithoutTrivia();
        Parameters = parameters;
    }

    public MemberSignature(VariableDeclarationSyntax declaration)
    {
        Declaration = declaration;
    }

    public MemberSignature(SyntaxToken identifier)
    {
        Identifier = identifier;
    }

    public static MemberSignature FromDeclarationSyntax(MemberDeclarationSyntax syntax) => syntax switch
    {
        MethodDeclarationSyntax m => new MemberSignature(m.Identifier, m.ParameterList)
        {
            ExplicitInterfaceSpecifier = m.ExplicitInterfaceSpecifier
        },
        PropertyDeclarationSyntax p => new MemberSignature(p.Identifier)
        {
            ExplicitInterfaceSpecifier = p.ExplicitInterfaceSpecifier
        },
        RecordDeclarationSyntax r => new MemberSignature(r.Identifier, r.ParameterList),
        FieldDeclarationSyntax f => new MemberSignature(f.Declaration),
        ConstructorDeclarationSyntax c => new MemberSignature(c.Identifier, c.ParameterList),
        EnumDeclarationSyntax e => new MemberSignature(e.Identifier),
        TypeDeclarationSyntax t => new MemberSignature(t.Identifier),
        IndexerDeclarationSyntax i => new MemberSignature(SyntaxFactory.Identifier("this"), i.ParameterList)
        {
            ExplicitInterfaceSpecifier = i.ExplicitInterfaceSpecifier
        },
        ConversionOperatorDeclarationSyntax c => new MemberSignature(c.ImplicitOrExplicitKeyword, c.ParameterList)
        {
            ExplicitInterfaceSpecifier = c.ExplicitInterfaceSpecifier
        },
        OperatorDeclarationSyntax o => new MemberSignature(o.OperatorToken, o.ParameterList)
        {
            ExplicitInterfaceSpecifier = o.ExplicitInterfaceSpecifier
        },
        _ => throw new ArgumentException($"Invalid member type: {syntax.GetType().FullName}")
    };

    public override string ToString()
    {
        StringBuilder sb = new();

        if (ExplicitInterfaceSpecifier is not null)
        {
            sb.Append(ExplicitInterfaceSpecifier);
        }

        if (Identifier is not null)
        {
            sb.Append(Identifier);
        }

        if (Parameters is not null)
        {
            sb.Append("(");
            sb.Append(Parameters);
            sb.Append(")");
        }

        if (Declaration is not null)
        {
            sb.Append(Declaration.Type);
            foreach (VariableDeclaratorSyntax? variable in Declaration.Variables)
            {
                sb.Append(variable);
                sb.Append(";");
            }
        }

        return sb.ToString();
    }

    public bool Equals(MemberSignature other) =>
        ExplicitInterfaceSpecifier?.ToString() == other.ExplicitInterfaceSpecifier?.ToString() &&
        Identifier.ToString() == other.Identifier.ToString() &&
        Parameters?.ToString() == other.Parameters?.ToString() &&
        Declaration?.ToString() == other.Declaration?.ToString();

    public override bool Equals(object? obj)
    {
        return obj is MemberSignature other && Equals(other);
    }

    public override int GetHashCode()
    {
        int hashCode = 0;
        string? explicitInterfaceSpecifier = ExplicitInterfaceSpecifier?.ToString();
        string? identifier = Identifier?.ToString();
        string? parameters = Parameters?.ToString();
        string? declaration = Declaration?.ToString();
        unchecked
        {
            Combine(ref hashCode, explicitInterfaceSpecifier?.GetHashCode() ?? 0);
            Combine(ref hashCode, identifier?.GetHashCode() ?? 0);
            Combine(ref hashCode, parameters?.GetHashCode() ?? 0);
            Combine(ref hashCode, declaration?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    static void Combine(ref int accumulator, int hashCode) =>
        accumulator = (accumulator*397) ^ hashCode;
}