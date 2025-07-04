using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator;

public static class MethodDeclarationSyntaxEx
{
    public static IEnumerable<MemberDeclarationSyntax> ResolveGenerics(
        this IEnumerable<MemberDeclarationSyntax> memberSyntax,
        BaseTypeSyntax baseTypeSyntax,
        InterfaceDeclarationSyntax interfaceSyntax)
    {
        foreach (var syntax in memberSyntax)
        {
            if (syntax is MethodDeclarationSyntax methodSyntax)
                yield return ResolveGenericInterfaceMethod(methodSyntax, baseTypeSyntax, interfaceSyntax);
            else yield return syntax;
        }
    }

    public static MemberDeclarationSyntax ResolveGenericInterfaceMethod(
        this MethodDeclarationSyntax methodSyntax,
        BaseTypeSyntax baseTypeSyntax,
        InterfaceDeclarationSyntax interfaceSyntax)
    {
        var newMethodSyntax = methodSyntax;
        var baseTypeArgs = baseTypeSyntax.FirstChildNode<GenericNameSyntax>().TypeArgumentList.Arguments;
        var interfaceArgs = interfaceSyntax.TypeParameterList?.Parameters ?? [];
        if (newMethodSyntax.ReturnType is GenericNameSyntax genericNameSyntax)
        {
            var returnArgs = genericNameSyntax.TypeArgumentList.Arguments;
            var newReturnArgs = returnArgs.ToArray();

            for (var i = 0; i < returnArgs.Count; i++)
            {
                var returnArg = returnArgs[i];
                for (var j = 0; j < interfaceArgs.Count; j++)
                {
                    var interfaceArg = interfaceArgs[j];
                    if (returnArg.ToString() == interfaceArg.ToString())
                        newReturnArgs[i] = baseTypeArgs[j];
                }
            }

            newMethodSyntax = newMethodSyntax.WithReturnType(genericNameSyntax.WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList(newReturnArgs)
                )
            ));
        }

        if (newMethodSyntax.Body is {} blockSyntax)
        {
            blockSyntax.VisitNodesDepthFirst(node =>
            {
                foreach (var interfaceArg in interfaceArgs)
                foreach (var baseTypeArg in baseTypeArgs)
                    if (node.ToString() == interfaceArg.ToString())
                        newMethodSyntax = newMethodSyntax.ReplaceNode(node, baseTypeArg);
            });
        }

        if (newMethodSyntax.ExpressionBody is {} expressionSyntax)
        {
            expressionSyntax.Expression.VisitNodesDepthFirst(node =>
            {
                foreach (var interfaceArg in interfaceArgs)
                foreach (var baseTypeArg in baseTypeArgs)
                    if (node.ToString() == interfaceArg.ToString())
                        newMethodSyntax = newMethodSyntax.ReplaceNode(node, baseTypeArg);
            });
        }

        return (newMethodSyntax ?? methodSyntax)
            .NormalizeWhitespace()
            .WithTrailingTrivia(methodSyntax.GetTrailingTrivia());
    }
}