using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Roslyn.Shared.Helpers;
using static Robust.Roslyn.Shared.DataDefinitionHelper;

namespace Robust.Serialization.Generator;

internal static class Types
{
    private const string CopyByRefNamespace = "Robust.Shared.Serialization.Manager.Attributes.CopyByRefAttribute";

    internal static bool IsPartial(TypeDeclarationSyntax type)
    {
        return type.Modifiers.IndexOf(SyntaxKind.PartialKeyword) != -1;
    }

    internal static IEnumerable<string> GetImplicitDataDefinitionInterfaces(ITypeSymbol type, bool all)
    {
        var interfaces = all ? type.AllInterfaces : type.Interfaces;
        foreach (var @interface in interfaces)
        {
            if (IsImplicitDataDefinitionInterface(@interface) is { Definition: true })
                yield return @interface.ToDisplayString();
        }
    }

    internal static bool IsNullableType(ITypeSymbol type)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
            return true;

        if (type.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
            return true;

        return false;
    }

    internal static bool IsNullableValueType(ITypeSymbol type)
    {
        return type.IsValueType && IsNullableType(type);
    }

    internal static bool IsMultidimensionalArray(ITypeSymbol type)
    {
        return type is IArrayTypeSymbol { Rank: > 1 };
    }

    internal static bool CanBeCopiedByValue(ISymbol member, ITypeSymbol type)
    {
        if (type.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
            return CanBeCopiedByValue(member, ((INamedTypeSymbol) type).TypeArguments[0]);

        if (type.TypeKind == TypeKind.Enum)
            return true;

        switch (type.SpecialType)
        {
            case SpecialType.System_Enum:
            case SpecialType.System_Boolean:
            case SpecialType.System_Char:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_String:
            case SpecialType.System_DateTime:
                return true;
        }

        if (HasAttribute(member, CopyByRefNamespace))
            return true;

        return false;
    }

    internal static string GetGenericTypeName(ITypeSymbol symbol)
    {
        var name = symbol.Name;

        if (symbol is INamedTypeSymbol { TypeParameters: { Length: > 0 } parameters })
        {
            name += "<";

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                name += parameter.Name;

                if (i < parameters.Length - 1)
                {
                    name += ", ";
                }
            }

            name += ">";
        }

        return name;
    }

    internal static string GetPartialTypeDefinitionLine(ITypeSymbol symbol)
    {
        var access = symbol.DeclaredAccessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "protected internal",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.Public => "public",
            _ => "public"
        };

        var typeKeyword = "partial ";
        if (symbol.TypeKind == TypeKind.Interface)
        {
            typeKeyword += "interface";
        }
        else
        {
            if (symbol.IsRecord)
            {
                typeKeyword += symbol.IsValueType ? "record struct" : "record";
            }
            else
            {
                typeKeyword += symbol.IsValueType ? "struct" : "class";
            }

            if (symbol.IsAbstract)
            {
                typeKeyword = $"abstract {typeKeyword}";
            }
        }

        var typeName = GetGenericTypeName(symbol);
        return $"{access} {typeKeyword} {typeName}";
    }

    internal static bool ImplementsInterface(ITypeSymbol type, string interfaceName, List<INamedTypeSymbol> symbols)
    {
        symbols.Clear();
        foreach (var interfaceType in type.AllInterfaces)
        {
            if (interfaceType.ToDisplayString().Contains(interfaceName))
                symbols.Add(interfaceType);

            if (interfaceType.BaseType is { } baseInterface &&
                ImplementsInterface(baseInterface, interfaceName, symbols))
            {
                return true;
            }
        }

        return symbols.Count > 0;
    }

    internal static bool ImplementsInterface(ITypeSymbol type, string interfaceName)
    {
        var symbols = new List<INamedTypeSymbol>();
        return ImplementsInterface(type, interfaceName, symbols);
    }

    internal static bool IsReadOnlyMember(ITypeSymbol type, ISymbol member)
    {
        if (member is IFieldSymbol field)
        {
            return field.IsReadOnly;
        }

        if (member is IPropertySymbol property)
        {
            if (property.SetMethod == null)
                return true;

            if (property.SetMethod.IsInitOnly)
                return type.IsReferenceType;
        }

        return false;
    }

    internal static (bool NeedsEmpty, IMethodSymbol? MustCall) NeedsEmptyConstructor(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
            return (false, null);

        if (named.InstanceConstructors.Length == 0 ||
            named.InstanceConstructors.All(c => c.IsImplicitlyDeclared))
        {
            return (true, null);
        }

        var needsEmpty = true;
        IMethodSymbol? mustCall = null;
        foreach (var constructor in named.InstanceConstructors)
        {
            if (constructor.IsImplicitlyDeclared)
                continue;

            if (constructor.Parameters.Length == 0)
                needsEmpty = false;

            if (mustCall != null)
                continue;

            // Is there a better way to find a primary constructor? I don't know! The docs don't tell you!
            // Neither does Google, because all the results are useless SEO-optimized AI garbage!
            // I don't think you can even access the underlying symbol directly! Hooray!
            // So we get the syntax's nodes and find out
            foreach (var syntax in constructor.DeclaringSyntaxReferences)
            {
                var nodes = syntax.GetSyntax().DescendantNodesAndSelf().ToArray();
                if (nodes.Length == 0 || nodes[0] is not TypeDeclarationSyntax)
                    continue;

                if (nodes.Any(n => n is ParameterListSyntax))
                {
                    mustCall = constructor;
                    break;
                }
            }
        }

        return (needsEmpty, mustCall);
    }

    internal static bool IsVirtualClass(ITypeSymbol type)
    {
        return type.IsReferenceType && !type.IsSealed && type.TypeKind != TypeKind.Interface;
    }

    internal static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == attributeName)
                return true;
        }

        return false;
    }

    internal static bool TryGetAttribute(ISymbol symbol, string attributeName, [NotNullWhen(true)] out AttributeData? data)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == attributeName)
            {
                data = attribute;
                return true;
            }
        }

        data = null;
        return false;
    }

    internal static IEnumerable<ITypeSymbol> GetBaseTypes(ITypeSymbol type)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            yield return baseType;
            baseType = baseType.BaseType;
        }
    }

    internal static (string Flat, string NonNullable) GetCleanNameForGenericType(ITypeSymbol type, out bool isNullableValueType)
    {
        var typeName = type.ToDisplayString();
        if (IsMultidimensionalArray(type))
            typeName = typeName.Replace("*", "");

        isNullableValueType = IsNullableValueType(type);
        var nonNullableTypeName = type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
        if (isNullableValueType)
            nonNullableTypeName = typeName.Substring(0, typeName.Length - 1);

        return (typeName, nonNullableTypeName);
    }

    internal static string GetNonNullableNameForGenericParameter(ITypeSymbol type)
    {
        var typeName = type.ToDisplayString();
        if (IsMultidimensionalArray(type))
            typeName = typeName.Replace("*", "");

        if (typeName.EndsWith("?"))
            typeName = typeName.Substring(0, typeName.Length - 1);

        return typeName;
    }

    internal static IEnumerable<ISymbol> GetRequiredFieldsProperties(ITypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member.IsImplicitlyDeclared)
                continue;

            if (member is not IFieldSymbol { IsRequired: true } &&
                member is not IPropertySymbol { IsRequired: true })
            {
                continue;
            }

            yield return member;
        }
    }

    internal static string GetRequiredFieldsPropertiesAssigners(ITypeSymbol type, string accessor)
    {
        var requiredFields = new StringBuilder();
        foreach (var member in GetRequiredFieldsProperties(type))
        {
            // Yes you can just set the field to itself to bypass the compiler's required check
            // I don't know man
            // Old dynamic method serialization did not change their values
            // So we just do this
            requiredFields.AppendLine($"{member.Name} = {accessor}{member.Name}!,");
        }

        if (requiredFields.Length > 0)
        {
            requiredFields.Insert(0, '{');
            requiredFields.Append('}');
        }

        return requiredFields.ToString();
    }

    internal static string GetSetsRequiredAttributeOrEmpty(ITypeSymbol type)
    {
        var setsRequired = string.Empty;
        if (GetRequiredFieldsProperties(type).Any())
            setsRequired = "[SetsRequiredMembers]";

        return setsRequired;
    }

    internal static ITypeSymbol? GetFirstDataDefinitionBaseType(ITypeSymbol type)
    {
        var parent = type;
        while ((parent = parent.BaseType) != null)
        {
            if (IsDataDefinition(parent, out _))
                return parent;
        }

        return null;
    }

    internal static IEnumerable<(ISymbol Field, ITypeSymbol Type, DataFieldAttribute Attribute)> GetAllDataFields(ITypeSymbol? definition, bool isDataRecord)
    {
        while (definition != null)
        {
            foreach (var member in definition.GetMembers())
            {
                if (member is not IFieldSymbol && member is not IPropertySymbol)
                    continue;

                if (member.IsStatic)
                    continue;

                if (!IsDataField(member, isDataRecord, out var type, out var attribute))
                    continue;

                yield return (member, type, attribute);
            }

            definition = definition.BaseType;
        }
    }
}
