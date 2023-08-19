using Microsoft.CodeAnalysis;

namespace Robust.Serialization.Generator;

internal static class Types
{
    private const string DataFieldBaseNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataFieldBaseAttribute";
    private const string DataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataDefinitionAttribute";
    private const string ImplicitDataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.ImplicitDataDefinitionForInheritorsAttribute";

    internal static bool IsDataField(ISymbol member, out ITypeSymbol type, out AttributeData attribute)
    {
        // TODO data records
        if (member is IFieldSymbol field)
        {
            foreach (var attr in field.GetAttributes())
            {
                if (attr.AttributeClass != null && Inherits(attr.AttributeClass, DataFieldBaseNamespace))
                {
                    type = field.Type;
                    attribute = attr;
                    return true;
                }
            }
        }
        else if (member is IPropertySymbol property)
        {
            foreach (var attr in property.GetAttributes())
            {
                if (attr.AttributeClass != null && Inherits(attr.AttributeClass, DataFieldBaseNamespace))
                {
                    type = property.Type;
                    attribute = attr;
                    return true;
                }
            }
        }

        type = null!;
        attribute = null!;
        return false;
    }

    internal static bool IsDataDefinition(ITypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == DataDefinitionNamespace)
                return true;
        }

        var baseType = type.BaseType;
        while (baseType != null)
        {
            foreach (var attribute in baseType.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == ImplicitDataDefinitionNamespace)
                    return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    internal static bool IsNullableType(ITypeSymbol type)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
            return true;

        if (type.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
            return true;

        return false;
    }

    internal static bool CanBeCopiedByValue(ITypeSymbol type)
    {
        if (type.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
            return CanBeCopiedByValue(((INamedTypeSymbol) type).TypeArguments[0]);

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
            default:
                return false;
        }
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

        string typeKeyword;
        if (symbol.IsRecord)
        {
            typeKeyword = symbol.IsValueType ? "record struct" : "record";
        }
        else
        {
            typeKeyword = symbol.IsValueType ? "struct" : "class";
        }

        var abstractKeyword = symbol.IsAbstract ? "abstract " : string.Empty;
        var typeName = GetGenericTypeName(symbol);
        return $"{access} {abstractKeyword}partial {typeKeyword} {typeName}";
    }

    internal static bool Inherits(ITypeSymbol type, string parent)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.ToDisplayString() == parent)
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    internal static bool ImplementsInterface(ITypeSymbol type, string interfaceName)
    {
        foreach (var @interface in type.AllInterfaces)
        {
            if (@interface.ToDisplayString().Contains(interfaceName))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsReadOnlyMember(ISymbol member)
    {
        if (member is IFieldSymbol field)
        {
            return field.IsReadOnly;
        }
        else if (member is IPropertySymbol property)
        {
            return property.SetMethod == null;
        }

        return false;
    }

    internal static bool NeedsEmptyConstructor(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        if (named.InstanceConstructors.Length == 0)
            return true;

        foreach (var constructor in named.InstanceConstructors)
        {
            if (constructor.Parameters.Length == 0 && !constructor.IsImplicitlyDeclared)
                return false;
        }

        return true;
    }
}
