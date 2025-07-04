using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemberGenerator;

public static class InterfaceDeclarationSyntaxEx
{
    public static IEnumerable<MemberDeclarationSyntax> GetDefaultInterfaceMembers(
        this InterfaceDeclarationSyntax syntax)
    {
        return syntax.Members
            .Where(memberSyntax => memberSyntax switch
            {
                MethodDeclarationSyntax { Body: not null } => true,
                MethodDeclarationSyntax { ExpressionBody: not null } => true,
                PropertyDeclarationSyntax { ExpressionBody: not null } => true,
                _ => false,
            });
    }
}