using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using MemberGenerator.Ex;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MemberGenerator;

[Generator]
public class MemberGenerator : IIncrementalGenerator
{
    const string Namespace     = "MemberGenerator";
    const string AttributeName = "GenerateDefaultMembers";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var codeUnits = GeneratePartialTypes(context);
        context.RegisterPostInitializationOutput(EmitAttributes);
        context.RegisterSourceOutput(codeUnits, EmitPartialTypes);
    }

    static IncrementalValueProvider<ImmutableArray<CodeUnit>> GeneratePartialTypes(
        IncrementalGeneratorInitializationContext context)
    {
        var annotatedInterfaces = context.SyntaxProvider.ForAttributeWithMetadataName(
                $"{Namespace}.{AttributeName}Attribute",
                static (s, _) => s is InterfaceDeclarationSyntax,
                static (c, _) =>
                {
                    if (c.TargetSymbol is not INamedTypeSymbol interfaceSymbol)
                    {
                        Console.WriteLine($"TargetSymbol is not INamedTypeSymbol: {c.TargetSymbol}");
                        return null;
                    }

                    CSharpInterface cSharpInterface = new CSharpInterface(interfaceSymbol)
                    {
                        Syntaxes = interfaceSymbol.DeclaringSyntaxReferences
                            .Select(x => x.GetSyntax())
                            .OfType<InterfaceDeclarationSyntax>()
                            .ToReadonlyList()
                    };

                    return (CSharpType)cSharpInterface;
                })
            .Where(static state => state != null)
            .Collect()
            .Select(static (state, _) => state.OfType<CSharpType>()
                .ToImmutableHashSet(CSharpEqualityComparer.Instance)
            );

        var allPartialTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (syntax, _) => syntax is TypeDeclarationSyntax declaration
                                      && declaration.IsPartial()
                                      && declaration.HasBaseList(),
                static (context, cancellationToken) =>
                {
                    ISymbol? symbol = context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken);
                    if (symbol is not INamedTypeSymbol typeSymbol)
                    {
                        Console.WriteLine($"TypeDeclarationSyntax is not INamedTypeSymbol: {context.Node}");
                        return null;
                    }

                    var type = new CSharpType(typeSymbol)
                    {
                        Syntaxes = typeSymbol.DeclaringSyntaxReferences
                            .Select(x => x.GetSyntax())
                            .OfType<TypeDeclarationSyntax>()
                            .ToReadonlyList()
                    };
                    return type;
                })
            .Where(static typeSignature => typeSignature != null);

        var partialTypes = allPartialTypes
            .Combine(annotatedInterfaces)
            .Select(static (state, cancellationToken) =>
            {
                var type = state.Left!;
                var genericBaseTypes = type.Syntaxes.SelectMany(s => s.AncestorsAndSelf())
                    .OfType<TypeDeclarationSyntax>()
                    .SelectMany(t => t.BaseList?.Types.AsEnumerable() ?? [])
                    .Where(b => b.Type is GenericNameSyntax)
                    .ToArray();

                var newType = type with { Members = ImmutableArray<CSharpMember>.Empty };
                var genericMembersDictionary = state.Right;

                var comparer = CSharpEqualityComparer.Instance;
                var interfaceMap = new Dictionary<CSharpInterface, Dictionary<string, string>>(comparer);

                foreach (var csInterface in type.AllInterfaces)
                {
                    var genericMap = new Dictionary<string, string>();
                    foreach (var baseTypeSyntax in genericBaseTypes)
                    {
                        var generic = (GenericNameSyntax)baseTypeSyntax.Type;
                        
                        if (generic.Arity != csInterface.TypeParametersTypeSyntaxes.Length)
                            continue;
                        
                        foreach (var interfaceType in csInterface.TypeParametersTypeSyntaxes)
                        {
                            string keyString = interfaceType.ToString();
                            foreach (var baseArg in generic.TypeArgumentList.Arguments)
                            {
                                string valueString = baseArg.ToString();

                                if (!genericMap.ContainsKey(keyString))
                                    genericMap.Add(keyString, valueString);
                            }
                        }
                    }

                    if (!interfaceMap.ContainsKey(csInterface))
                        interfaceMap.Add(csInterface, genericMap);
                }

                foreach (var csInterface in type.AllInterfaces)
                {
                    var genericMap = interfaceMap[csInterface];
                    if (cancellationToken.IsCancellationRequested) break;

                    if (genericMembersDictionary.Contains(csInterface))
                    {
                        var equalityComparer = CSharpEqualityComparer.Instance;

                        var defaultMembers = csInterface.WithTypesQualified().Members
                            .WithSyntaxWhere(MembersEx.HasBodyOrExpressionBody)
                            .Select(c => c.GenericSubstitution(genericMap));

                        var newMembers = defaultMembers.Except(type.Members, equalityComparer).ToImmutableArray();
                        newType = newType with
                        {
                            Members = newType.Members.Length switch
                            {
                                0 => newMembers,
                                _ => newType.Members.Union(newMembers, equalityComparer).ToImmutableArray()
                            }
                        };
                    }
                }

                return newType;
            }).Collect();

        var codeUnits = partialTypes.Select(static (csTypes, _) =>
        {
            var sb = new StringBuilder();
            var builder = ImmutableArray.CreateBuilder<CodeUnit>();
            foreach (CSharpType csType in csTypes)
                builder.Add(BuildCodeAndClear(sb, csType));
            return builder.ToImmutable();
        });
        return codeUnits;
    }

    static void EmitPartialTypes(SourceProductionContext context, ImmutableArray<CodeUnit> codegen)
    {
        foreach (var (fileName, syntax) in codegen)
        {
            SourceText sourceCode = syntax.GetText(Encoding.UTF8);
            context.AddSource(fileName, sourceCode);
        }
    }

    static void EmitAttributes(IncrementalGeneratorPostInitializationContext ctx)
    {
        SourceText sourceCode = SourceText.From(
            // language=C#
            $$"""
              // <auto-generated/>
              namespace {{Namespace}}
              {
                  [System.AttributeUsage(System.AttributeTargets.Interface)]
                  internal class {{AttributeName}}Attribute : System.Attribute
                  {
                  }
              };
              """, Encoding.UTF8
        );
        ctx.AddSource($"{AttributeName}Attribute.g.cs", sourceCode);
    }

    static CodeUnit BuildCodeAndClear(StringBuilder sb, CSharpType csType)
    {
        sb.Clear();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS0109");

        sb.AppendParentNamespace(csType.Symbol);
        sb.AppendLine();

        var usingNamespaces = csType.Members.SyntaxDeclarations()
            .SelectMany(t => t.SyntaxTree.GetRoot().DescendantNodesAndSelf())
            .OfType<UsingDirectiveSyntax>()
            .OrderBy(_ => "global::")
            .Distinct();

        foreach (var usingNamespace in usingNamespaces)
            sb.AppendLine(usingNamespace.ToString());

        sb.AppendParentTypesBegin(csType.Symbol);
        sb.AppendTypeIdentifierName(csType.Symbol);
        sb.AppendLine("{");

        var memberDisplayFormat = SymbolDisplayEx.IdentifierDisplayFormat.With(
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            memberOptions: SymbolDisplayMemberOptions.None
                           | SymbolDisplayMemberOptions.IncludeParameters
                           | SymbolDisplayMemberOptions.IncludeType
                           | SymbolDisplayMemberOptions.IncludeRef
                           | SymbolDisplayMemberOptions.IncludeExplicitInterface
                           | SymbolDisplayMemberOptions.IncludeModifiers
                           | SymbolDisplayMemberOptions.IncludeAccessibility
        );

        foreach (var csMember in csType.Members)
        {
            if (csMember.Syntaxes.FirstOrDefault() is null) continue;

            if (csMember.Symbol is IMethodSymbol { IsAsync: true })
                sb.Append("async ");

            var isExplicitInterface =
                csMember.Symbol
                    is IMethodSymbol { ExplicitInterfaceImplementations.Length: > 0 }
                    or IPropertySymbol { ExplicitInterfaceImplementations.Length: > 0 };

            if (!isExplicitInterface)
                sb.Append("public new ");

            sb.AppendLine(csMember.Symbol.ToDisplayString(memberDisplayFormat));

            sb.AppendLine();

            if (csMember.Body is {} body)
            {
                sb.AppendLine(body.ToString());
                if (body is ArrowExpressionClauseSyntax) sb.AppendLine(";");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendParentTypesEnd(csType.Symbol);
        sb.AppendLine("}");
        sb.AppendLine("#pragma warning restore CS0109");
        string fileName = csType.FileNameString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var tree = CSharpSyntaxTree.ParseText(sb.ToString());
        var root = tree.GetRoot().NormalizeWhitespace();
        return new CodeUnit(fileName, root);
    }
}

internal record struct CodeUnit(string Filename, SyntaxNode Code);