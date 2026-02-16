using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Robust.Serialization.Generator;

internal static class Types
{
    private const string DataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataDefinitionAttribute";
    private const string ImplicitDataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.ImplicitDataDefinitionForInheritorsAttribute";
    private const string DataFieldBaseNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataFieldBaseAttribute";
    private const string CopyByRefNamespace = "Robust.Shared.Serialization.Manager.Attributes.CopyByRefAttribute";
    private const string DataFieldAttributeName = "Robust.Shared.Serialization.Manager.Attributes.DataFieldAttribute";
    private const string IdDataFieldAttributeName = "Robust.Shared.Prototypes.IdDataFieldAttribute";
    private const string ParentDataFieldAttributeName = "Robust.Shared.Prototypes.ParentDataFieldAttribute";
    private const string AbstractDataFieldAttributeName = "Robust.Shared.Prototypes.AbstractDataFieldAttribute";
    private const string IncludeDataFieldAttributeName = "Robust.Shared.Serialization.Manager.Attributes.IncludeDataFieldAttribute";

    internal static bool IsPartial(TypeDeclarationSyntax type)
    {
        return type.Modifiers.IndexOf(SyntaxKind.PartialKeyword) != -1;
    }

    internal static bool IsDataDefinition([NotNullWhen(true)] ITypeSymbol? type)
    {
        if (type == null)
            return false;

        return HasAttribute(type, DataDefinitionNamespace) ||
               IsImplicitDataDefinition(type);
    }

    internal static DataFieldAttribute? GetDataFieldAttribute(AttributeData data, string fieldName)
    {
        if (data.AttributeClass == null)
            return null;

        string? name = null;
        var readOnly = false;
        var priorityIndex = 0;
        var include = false;
        var isDataFieldAttribute = false;
        var required = false;
        var serverOnly = false;

        // (string? tag = null, bool readOnly = false, int priority = 1, bool required = false, bool serverOnly = false, Type? customTypeSerializer = null)
        if (data.AttributeClass.ToDisplayString().Contains(DataFieldAttributeName))
        {
            name = (string?) data.ConstructorArguments[0].Value;
            readOnly = (bool) data.ConstructorArguments[1].Value!;
            priorityIndex = 2;
            isDataFieldAttribute = true;
            required = (bool) data.ConstructorArguments[3].Value!;
            serverOnly = (bool) data.ConstructorArguments[4].Value!;
        }
        // (int priority = 1, Type? customTypeSerializer = null)
        else if (data.AttributeClass.ToDisplayString().Contains(IdDataFieldAttributeName))
        {
            name = "id";
            priorityIndex = 0;
            isDataFieldAttribute = true;
        }
        // (Type prototypeIdSerializer, int priority = 1)
        else if (data.AttributeClass.ToDisplayString().Contains(ParentDataFieldAttributeName))
        {
            name = "parent";
            priorityIndex = 1;
            isDataFieldAttribute = true;
        }
        // (int priority = 1)
        else if (data.AttributeClass.ToDisplayString().Contains(AbstractDataFieldAttributeName))
        {
            name = "abstract";
            priorityIndex = 0;
            isDataFieldAttribute = true;
        }
        // (bool readOnly = false, int priority = 1, bool serverOnly = false, Type? customTypeSerializer = null)
        else if (data.AttributeClass.ToDisplayString().Contains(IncludeDataFieldAttributeName))
        {
            var span = fieldName.AsSpan();
            name = $"{char.ToLowerInvariant(span[0])}{span.Slice(1).ToString()}";
            readOnly = (bool) data.ConstructorArguments[0].Value!;
            priorityIndex = 1;
            include = true;
            serverOnly = (bool) data.ConstructorArguments[2].Value!;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            var span = fieldName.AsSpan();
            name = $"{char.ToLowerInvariant(span[0])}{span.Slice(1).ToString()}";
        }

        return new DataFieldAttribute(
            data,
            name!,
            readOnly,
            (int) data.ConstructorArguments[priorityIndex].Value!,
            include,
            isDataFieldAttribute,
            required,
            serverOnly
        );
    }

    internal static bool IsDataField(ISymbol member, out ITypeSymbol type, [NotNullWhen(true)] out DataFieldAttribute? attribute)
    {
        // TODO data records and other attributes
        type = null!;
        attribute = null;
        if (member is IFieldSymbol field)
        {
            foreach (var attr in field.GetAttributes())
            {
                if (attr.AttributeClass != null &&
                    Inherits(attr.AttributeClass, DataFieldBaseNamespace))
                {
                    type = field.Type;
                    attribute = GetDataFieldAttribute(attr, field.Name);
                    break;
                }
            }
        }
        else if (member is IPropertySymbol property)
        {
            foreach (var attr in property.GetAttributes())
            {
                if (attr.AttributeClass != null &&
                    Inherits(attr.AttributeClass, DataFieldBaseNamespace))
                {
                    type = property.Type;
                    attribute = GetDataFieldAttribute(attr, property.Name);
                    break;
                }
            }
        }

        return attribute != null;
    }

    internal static bool IsImplicitDataDefinition(ITypeSymbol type)
    {
        if (HasAttribute(type, ImplicitDataDefinitionNamespace))
            return true;

        foreach (var baseType in GetBaseTypes(type))
        {
            if (HasAttribute(baseType, ImplicitDataDefinitionNamespace))
                return true;
        }

        foreach (var @interface in type.AllInterfaces)
        {
            if (IsImplicitDataDefinitionInterface(@interface))
                return true;
        }

        return false;
    }

    internal static bool IsImplicitDataDefinitionInterface(ITypeSymbol @interface)
    {
        if (HasAttribute(@interface, ImplicitDataDefinitionNamespace))
            return true;

        foreach (var subInterface in @interface.AllInterfaces)
        {
            if (HasAttribute(subInterface, ImplicitDataDefinitionNamespace))
                return true;
        }

        return false;
    }

    internal static IEnumerable<string> GetImplicitDataDefinitionInterfaces(ITypeSymbol type, bool all)
    {
        var interfaces = all ? type.AllInterfaces : type.Interfaces;
        foreach (var @interface in interfaces)
        {
            if (IsImplicitDataDefinitionInterface(@interface))
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

    internal static bool Inherits(ITypeSymbol type, string parent)
    {
        foreach (var baseType in GetBaseTypes(type))
        {
            if (baseType.ToDisplayString() == parent)
                return true;
        }

        return false;
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
        else if (member is IPropertySymbol property)
        {
            if (property.SetMethod == null)
                return true;

            if (property.SetMethod.IsInitOnly)
                return type.IsReferenceType;

            return false;
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
            if (constructor.Parameters.Length == 0 &&
                !constructor.IsImplicitlyDeclared)
            {
                return false;
            }
        }

        return true;
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
}
