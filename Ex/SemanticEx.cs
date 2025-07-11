using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator.Ex;

public static class SemanticEx
{
    public static string? GetFullyQualifiedName(this SemanticModel semanticModel, TypeDeclarationSyntax syntax)
    {
        var symbol = ModelExtensions.GetDeclaredSymbol(semanticModel, syntax);
        return symbol?.ToDisplayString();
    }

    public static string GetFullyQualifiedName(this SemanticModel semanticModel, TypeSyntax typeSyntax)
    {
        var symbol = ModelExtensions.GetTypeInfo(semanticModel, typeSyntax).Type;
        return symbol?.ToDisplayString() ?? typeSyntax.ToFullString();
    }
}

public static class Semantics
{
    public static TypeDeclarationSyntax FullyQualify(
        SemanticModel semanticModel,
        TypeDeclarationSyntax declaration)
    {
        declaration = declaration
            .WithConstraintClauses(FullyQualify(semanticModel, declaration.ConstraintClauses))
            .WithMembers(FullyQualify(semanticModel, declaration.Members));
        ;

        return declaration;
    }

    public static TypeSyntax FullyQualify(SemanticModel semanticModel, TypeSyntax typeSyntax) =>
        SyntaxFactory.ParseTypeName(semanticModel.GetFullyQualifiedName(typeSyntax));

    public static SyntaxList<TypeParameterConstraintClauseSyntax> FullyQualify(SemanticModel semanticModel,
        SyntaxList<TypeParameterConstraintClauseSyntax> constraints)
    {
        SyntaxList<TypeParameterConstraintClauseSyntax> newConstraints = default;
        foreach (var clause in constraints)
        {
            TypeParameterConstraintClauseSyntax newClause = SyntaxFactory.TypeParameterConstraintClause(clause.Name);
            foreach (var parameterConstraint in clause.Constraints)
            {
                if (parameterConstraint is TypeConstraintSyntax typeConstraint)
                {
                    var typeSyntax = typeConstraint.Type;
                    var newType = FullyQualify(semanticModel, typeSyntax);
                    var newParameterConstraint = SyntaxFactory.TypeConstraint(newType);
                    newClause = newClause.AddConstraints(newParameterConstraint);
                }
            }


            newConstraints = newConstraints.Add(newClause);
        }

        return newConstraints;
    }

    public static SyntaxList<MemberDeclarationSyntax> FullyQualify(
        SemanticModel semanticModel,
        SyntaxList<MemberDeclarationSyntax> members)
    {
        SyntaxList<MemberDeclarationSyntax> newMembers = default;
        foreach (var member in members)
            newMembers = newMembers.Add(FullyQualify(semanticModel, member));
        return newMembers;
    }

    public static MemberDeclarationSyntax FullyQualify(
        SemanticModel semanticModel,
        MemberDeclarationSyntax member)
    {
        if (member is MethodDeclarationSyntax method)
        {
            method = method.WithReturnType(FullyQualify(semanticModel, method.ReturnType));
            return method;
        }

        if (member is PropertyDeclarationSyntax property)
        {
            property = property.WithType(FullyQualify(semanticModel, property.Type));
            return property;
        }

        return member;
    }
}