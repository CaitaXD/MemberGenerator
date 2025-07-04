using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator;

public readonly struct MemberSignature : IEquatable<MemberSignature>
{
    public readonly SyntaxToken?               Identifier;
    public readonly ParameterListSyntax?       Parameters;
    public readonly VariableDeclarationSyntax? Declaration;

    public MemberSignature(SyntaxToken identifier, ParameterListSyntax? parameters)
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
        MethodDeclarationSyntax m => new MemberSignature(m.Identifier, m.ParameterList),
        PropertyDeclarationSyntax p => new MemberSignature(p.Identifier),
        RecordDeclarationSyntax r => new MemberSignature(r.Identifier, r.ParameterList),
        FieldDeclarationSyntax f => new MemberSignature(f.Declaration),
        ConstructorDeclarationSyntax c => new MemberSignature(c.Identifier, c.ParameterList),
        EnumDeclarationSyntax e => new MemberSignature(e.Identifier),
        TypeDeclarationSyntax t => new MemberSignature(t.Identifier),
        _ => throw new ArgumentException($"Invalid member type: {syntax.GetType().FullName}")
    };

    public override string ToString()
    {
        StringBuilder sb = new();

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
            foreach (var variable in Declaration.Variables)
            {
                sb.Append(variable);
                sb.Append(";");
            }
        }

        return sb.ToString();
    }

    public bool Equals(MemberSignature other) =>
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
        string? identifier = Identifier?.ToString();
        string? parameters = Parameters?.ToString();
        string? declaration = Declaration?.ToString();
        unchecked
        {
            Combine(ref hashCode, identifier?.GetHashCode() ?? 0);
            Combine(ref hashCode, parameters?.GetHashCode() ?? 0);
            Combine(ref hashCode, declaration?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    static void Combine(ref int accumulator, int hashCode) =>
        accumulator = (accumulator*397) ^ hashCode;
}