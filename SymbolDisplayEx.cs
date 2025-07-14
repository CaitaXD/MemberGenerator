using Microsoft.CodeAnalysis;

namespace MemberGenerator;

public static class SymbolDisplayEx
{
    public static readonly SymbolDisplayFormat CodegenSymbolDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
                         | SymbolDisplayGenericsOptions.IncludeVariance
                         | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters
                       | SymbolDisplayMemberOptions.IncludeRef
                       | SymbolDisplayMemberOptions.IncludeType
                       | SymbolDisplayMemberOptions.IncludeContainingType,
        delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
        parameterOptions: SymbolDisplayParameterOptions.IncludeName
                          | SymbolDisplayParameterOptions.IncludeType
                          | SymbolDisplayParameterOptions.IncludeDefaultValue
                          | SymbolDisplayParameterOptions.IncludeParamsRefOut
                          | SymbolDisplayParameterOptions.IncludeExtensionThis,
        propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
        localOptions: SymbolDisplayLocalOptions.IncludeType,
        kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
                              | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );

    public static readonly SymbolDisplayFormat IdentifierDisplayFormat = CodegenSymbolDisplayFormat.With(
        memberOptions: CodegenSymbolDisplayFormat.MemberOptions & ~SymbolDisplayMemberOptions.IncludeContainingType
    );

    public static SymbolDisplayFormat With(
        this SymbolDisplayFormat format,
        SymbolDisplayGlobalNamespaceStyle? globalNamespaceStyle = null,
        SymbolDisplayTypeQualificationStyle? typeQualificationStyle = null,
        SymbolDisplayGenericsOptions? genericsOptions = null,
        SymbolDisplayMemberOptions? memberOptions = null,
        SymbolDisplayDelegateStyle? delegateStyle = null,
        SymbolDisplayExtensionMethodStyle? extensionMethodStyle = null,
        SymbolDisplayParameterOptions? parameterOptions = null,
        SymbolDisplayPropertyStyle? propertyStyle = null,
        SymbolDisplayLocalOptions? localOptions = null,
        SymbolDisplayKindOptions? kindOptions = null,
        SymbolDisplayMiscellaneousOptions? miscellaneousOptions = null) =>
        new(
            globalNamespaceStyle: globalNamespaceStyle ?? format.GlobalNamespaceStyle,
            typeQualificationStyle: typeQualificationStyle ?? format.TypeQualificationStyle,
            genericsOptions: genericsOptions ?? format.GenericsOptions,
            memberOptions: memberOptions ?? format.MemberOptions,
            delegateStyle: delegateStyle ?? format.DelegateStyle,
            extensionMethodStyle: extensionMethodStyle ?? format.ExtensionMethodStyle,
            parameterOptions: parameterOptions ?? format.ParameterOptions,
            propertyStyle: propertyStyle ?? format.PropertyStyle,
            localOptions: localOptions ?? format.LocalOptions,
            kindOptions: kindOptions ?? format.KindOptions,
            miscellaneousOptions: miscellaneousOptions ?? format.MiscellaneousOptions
        );
}