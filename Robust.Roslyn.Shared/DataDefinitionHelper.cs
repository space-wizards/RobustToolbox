#nullable enable
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Robust.Roslyn.Shared.Helpers;

namespace Robust.Roslyn.Shared;

public sealed class DataDefinitionHelper
{
    private const string DataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataDefinitionAttribute";
    private const string DataRecordNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataRecordAttribute";
    private const string ImplicitDataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.ImplicitDataDefinitionForInheritorsAttribute";
    private const string ImplicitDataRecordNamespace = "Robust.Shared.Serialization.Manager.Attributes.ImplicitDataRecordAttribute";
    private const string MeansDataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.MeansDataDefinitionAttribute";
    private const string MeansDataRecordNamespace = "Robust.Shared.Serialization.Manager.Attributes.MeansDataRecordAttribute";
    private const string DataFieldBaseNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataFieldBaseAttribute";
    private const string DataFieldAttributeName = "Robust.Shared.Serialization.Manager.Attributes.DataFieldAttribute";
    private const string IdDataFieldAttributeName = "Robust.Shared.Prototypes.IdDataFieldAttribute";
    private const string ParentDataFieldAttributeName = "Robust.Shared.Prototypes.ParentDataFieldAttribute";
    private const string AbstractDataFieldAttributeName = "Robust.Shared.Prototypes.AbstractDataFieldAttribute";
    private const string IncludeDataFieldAttributeName = "Robust.Shared.Serialization.Manager.Attributes.IncludeDataFieldAttribute";
    private const string AlwaysPushInheritanceAttributeName = "Robust.Shared.Serialization.Manager.Attributes.AlwaysPushInheritanceAttribute";
    private const string NeverPushInheritanceAttributeName = "Robust.Shared.Serialization.Manager.Attributes.NeverPushInheritanceAttribute";

    public static bool Inherits(ITypeSymbol type, string parent)
    {
        foreach (var baseType in GetBaseTypes(type))
        {
            if (baseType.ToDisplayString() == parent)
                return true;
        }

        return false;
    }

    public static IEnumerable<ITypeSymbol> GetBaseTypes(ITypeSymbol type)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            yield return baseType;
            baseType = baseType.BaseType;
        }
    }

    public static (bool Definition, bool Record) IsImplicitDataDefinitionInterface(ITypeSymbol @interface)
    {
        if (AttributeHelper.HasAttribute(@interface, ImplicitDataRecordNamespace))
            return (true, true);

        var isDefinition = false;
        foreach (var subInterface in @interface.AllInterfaces)
        {
            if (AttributeHelper.HasAttribute(subInterface, ImplicitDataRecordNamespace))
                return (true, true);

            if (AttributeHelper.HasAttribute(subInterface, ImplicitDataDefinitionNamespace))
                isDefinition = true;
        }

        return (isDefinition || AttributeHelper.HasAttribute(@interface, ImplicitDataDefinitionNamespace), false);
    }

    public static (bool Definition, bool Record) IsImplicitDataDefinition(ITypeSymbol type)
    {
        var isDefinition = false;
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass is not { } attributeClass)
                continue;

            var str = attributeClass.ToDisplayString();
            switch (str)
            {
                case ImplicitDataRecordNamespace:
                    return (true, true);
                case ImplicitDataDefinitionNamespace:
                    isDefinition = true;
                    break;
            }

            foreach (var subAttribute in attributeClass.GetAttributes())
            {
                if (subAttribute.AttributeClass is not { } subAttributeClass)
                    continue;

                var subStr = subAttributeClass.ToDisplayString();
                if (subStr is MeansDataRecordNamespace)
                    return (true, true);

                if (subStr is MeansDataDefinitionNamespace)
                    isDefinition = true;
            }
        }

        foreach (var baseType in GetBaseTypes(type))
        {
            if (AttributeHelper.HasAttribute(baseType, ImplicitDataRecordNamespace))
                return (true, true);

            if (AttributeHelper.HasAttribute(baseType, ImplicitDataDefinitionNamespace))
                isDefinition = true;
        }

        foreach (var @interface in type.AllInterfaces)
        {
            var impl = IsImplicitDataDefinitionInterface(@interface);
            if (impl.Record)
                return (true, true);

            if (impl.Definition)
                isDefinition = true;
        }

        return (isDefinition, false);
    }

    public static bool IsDataDefinition([NotNullWhen(true)] ITypeSymbol? type, out bool isDataRecord)
    {
        isDataRecord = false;
        if (type == null)
            return false;

        isDataRecord = AttributeHelper.HasAttribute(type, DataRecordNamespace);
        if (isDataRecord)
            return true;

        var (isImplicitDefinition, isImplicitRecord) = IsImplicitDataDefinition(type);
        if (isImplicitRecord)
            isDataRecord = true;

        return isImplicitDefinition ||
               isImplicitRecord ||
               AttributeHelper.HasAttribute(type, DataDefinitionNamespace);
    }


    public static DataFieldAttribute? GetDataFieldAttribute(AttributeData data, string fieldName)
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

        var camelCasedName = ToCamelCase(fieldName);
        if (string.IsNullOrWhiteSpace(name))
            name = ToCamelCase(camelCasedName);

        return new DataFieldAttribute(
            data,
            name!,
            readOnly,
            (int) data.ConstructorArguments[priorityIndex].Value!,
            include,
            isDataFieldAttribute,
            required,
            serverOnly,
            camelCasedName
        );
    }

    public static bool IsDataField(
        ImmutableArray<AttributeData> attributes,
        string name,
        bool isImplicitlyDeclared,
        bool isStatic,
        bool hasSetter,
        bool isRecord,
        out DataFieldAttribute? attribute,
        out int inheritanceBehavior)
    {
        attribute = null;
        inheritanceBehavior = 0;
        if (isStatic)
            return false;

        if (isImplicitlyDeclared && isRecord)
            return false;

        foreach (var attr in attributes)
        {
            if (attr.AttributeClass is not { } attributeClass)
                continue;

            if (Inherits(attributeClass, DataFieldBaseNamespace))
                attribute = GetDataFieldAttribute(attr, name);

            if (attributeClass.ToDisplayString() == AlwaysPushInheritanceAttributeName)
                inheritanceBehavior = 1;
            else if (attributeClass.ToDisplayString() == NeverPushInheritanceAttributeName)
                inheritanceBehavior = 2;
        }

        if (attribute != null)
            attribute = attribute with { InheritanceBehavior = inheritanceBehavior };

        if (isImplicitlyDeclared || !hasSetter || !isRecord)
            return attribute != null;

        name = ToCamelCase(name);
        attribute = new DataFieldAttribute(
            null,
            name,
            false,
            1,
            false,
            true,
            false,
            false,
            name,
            inheritanceBehavior
        );

        return true;
    }

    public static bool IsDataField(
        ISymbol member,
        bool isDataRecord,
        out ITypeSymbol type,
        [NotNullWhen(true)] out DataFieldAttribute? attribute)
    {
        // TODO data records and other attributes
        type = null!;
        attribute = null;
        var inheritanceBehavior = 0;
        switch (member)
        {
            case IFieldSymbol field:
            {
                if (IsDataField(field.GetAttributes(),
                        field.Name,
                        field.IsImplicitlyDeclared,
                        field.IsStatic,
                        true,
                        isDataRecord,
                        out attribute,
                        out inheritanceBehavior))
                    type = field.Type;

                break;
            }
            case IPropertySymbol property:
            {
                if (IsDataField(property.GetAttributes(),
                        property.Name,
                        property.IsImplicitlyDeclared,
                        property.IsStatic,
                        property.SetMethod != null,
                        isDataRecord,
                        out attribute,
                        out inheritanceBehavior))
                    type = property.Type;

                break;
            }
        }

        if (attribute != null)
            attribute = attribute with { InheritanceBehavior = inheritanceBehavior };

        return attribute != null;
    }

    public static string ToCamelCase(string name)
    {
        var span = name.AsSpan();
        return $"{char.ToLowerInvariant(span[0])}{span.Slice(1).ToString()}";
    }
}
