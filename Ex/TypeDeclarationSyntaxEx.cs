﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator.Ex;

public static class TypeDeclarationSyntaxEx
{
    public static bool SameArity(this TypeDeclarationSyntax declaration, TypeSyntax type) =>
        type is GenericNameSyntax generic && declaration.Arity == generic.Arity;

    public static TypeDeclarationSyntax WithPublicMembers(
        this TypeDeclarationSyntax syntax,
        params IEnumerable<MemberDeclarationSyntax> members) =>
        syntax switch
        {
            InterfaceDeclarationSyntax => syntax.WithMembers(
                SyntaxFactory.List(members.AddModifierIfNotExists(SyntaxFactory.Token(SyntaxKind.NewKeyword)))
            ),
            _ => syntax.WithMembers(SyntaxFactory.List(
                    members.Select(
                        member => member.ContainsExplicitInterfaceSpecifier()
                            ? member.RemoveModifier(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                            : member.AddModifierIfNotExists(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    )
                )
            ),
        };

    public static bool IsNestedType(this TypeDeclarationSyntax syntax) => syntax.Parent is TypeDeclarationSyntax;

    public static TypeDeclarationSyntax AddModifierIfNotExists(
        this TypeDeclarationSyntax syntax, SyntaxToken token) =>
        syntax.Modifiers.Any(t => t.IsKind(token.Kind()))
            ? syntax
            : syntax.WithModifiers(syntax.Modifiers.Add(token));
}