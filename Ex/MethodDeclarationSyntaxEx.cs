using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator.Ex;

public static class MethodDeclarationSyntaxEx
{
    public static IEnumerable<MemberDeclarationSyntax> ResolveGenerics(
        this IEnumerable<MemberDeclarationSyntax> memberSyntax,
        GenericNameSyntax genericNameSyntax,
        InterfaceDeclarationSyntax interfaceSyntax)
    {
        foreach (MemberDeclarationSyntax? syntax in memberSyntax)
        {
            if (syntax is MethodDeclarationSyntax methodSyntax)
                yield return ResolveGenericInterfaceMethod(methodSyntax, genericNameSyntax, interfaceSyntax);
            else yield return syntax;
        }
    }

    public static MemberDeclarationSyntax ResolveGenericInterfaceMethod(this MethodDeclarationSyntax methodSyntax,
        GenericNameSyntax genericNameSyntax,
        InterfaceDeclarationSyntax interfaceSyntax)
    {
        MethodDeclarationSyntax? newMethodSyntax = methodSyntax;
        var baseTypeArgs = genericNameSyntax.TypeArgumentList.Arguments;
        var interfaceArgs = interfaceSyntax.TypeParameterList?.Parameters ?? [];

        if (newMethodSyntax.ExplicitInterfaceSpecifier is {} explicitInterfaceSpecifier)
        {
            if (explicitInterfaceSpecifier.Name is GenericNameSyntax genericInterfaceSpecifier)
            {
                GenericNameSyntax? newName = ResolveGenericNameSyntax(genericInterfaceSpecifier);
                newMethodSyntax = newMethodSyntax.WithExplicitInterfaceSpecifier(
                    explicitInterfaceSpecifier.WithName(newName)
                );
            }
        }


        if (newMethodSyntax.ParameterList is { Parameters: { Count: > 0 } parameters })
        {
            foreach (ParameterSyntax? parameter in parameters)
            {
                if (parameter.Type is GenericNameSyntax genericParameter)
                {
                    GenericNameSyntax? newParameter = ResolveGenericNameSyntax(genericParameter);
                    newMethodSyntax = newMethodSyntax
                        .WithParameterList(
                            newMethodSyntax.ParameterList.WithParameters(
                                parameters.Replace(
                                    parameter, parameter.WithType(newParameter)
                                )
                            )
                        );
                }
            }
        }

        if (newMethodSyntax.ReturnType is GenericNameSyntax genericReturnType)
        {
            GenericNameSyntax? newReturnType = ResolveGenericNameSyntax(genericReturnType);
            newMethodSyntax = newMethodSyntax.WithReturnType(newReturnType);
        }

        if (newMethodSyntax.Body is {} blockSyntax)
        {
            blockSyntax.VisitNodesDepthFirst(ResolveGenericExpression);
        }

        if (newMethodSyntax.ExpressionBody is {} expressionSyntax)
        {
            expressionSyntax.Expression.VisitNodesDepthFirst(ResolveGenericExpression);
        }

        return (newMethodSyntax ?? methodSyntax)
            .NormalizeWhitespace()
            .WithTrailingTrivia(methodSyntax.GetTrailingTrivia());

        void ResolveGenericExpression(SyntaxNode node)
        {
            foreach (TypeParameterSyntax? interfaceArg in interfaceArgs)
            foreach (TypeSyntax? baseTypeArg in baseTypeArgs)
                if (node.ToString() == interfaceArg.ToString())
                    newMethodSyntax = newMethodSyntax.ReplaceNode(node, baseTypeArg);
        }

        GenericNameSyntax ResolveGenericNameSyntax(GenericNameSyntax genericName)
        {
            var typeArgs = genericName.TypeArgumentList.Arguments;
            var newReturnArgs = typeArgs.ToArray();

            for (var i = 0; i < typeArgs.Count; i++)
            {
                TypeSyntax? returnArg = typeArgs[i];
                for (var j = 0; j < interfaceArgs.Count; j++)
                {
                    TypeParameterSyntax? interfaceArg = interfaceArgs[j];
                    if (returnArg.ToString() == interfaceArg.Identifier.Text)
                        newReturnArgs[i] = baseTypeArgs[j];
                }
            }

            return genericName.WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList(newReturnArgs)
                )
            );
        }
    }
}