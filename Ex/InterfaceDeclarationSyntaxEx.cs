using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator.Ex;

public static class InterfaceDeclarationSyntaxEx
{
    public static bool NameEquals(this InterfaceDeclarationSyntax declaration, TypeSyntax type)
    {
        string typeName = type.ToString();
        string interfaceName = declaration.Identifier.Text;
        return interfaceName == typeName || declaration.SameArity(type) && typeName.StartsWith(interfaceName);
    }

    public static IEnumerable<MemberDeclarationSyntax> GetDefaultMembers(
        this InterfaceDeclarationSyntax syntax) =>
        syntax.Members.Where(memberSyntax => memberSyntax switch
        {
            MethodDeclarationSyntax { Body: not null } => true,
            MethodDeclarationSyntax { ExpressionBody: not null } => true,
            PropertyDeclarationSyntax { ExpressionBody: not null } => true,
            _ => false,
        });

    public static IEnumerable<MemberDeclarationSyntax> GetDefaultMembers(
        this IEnumerable<InterfaceDeclarationSyntax> syntax) => syntax.SelectMany(GetDefaultMembers);
}