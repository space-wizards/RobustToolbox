#nullable enable
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        foreach (var baseType in TypeSymbolHelper.GetBaseTypes(type))
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
        var priority = 1;
        var include = false;
        var isDataFieldAttribute = false;
        var required = false;
        var serverOnly = false;

        // (string? tag = null, bool readOnly = false, int priority = 1, bool required = false, bool serverOnly = false, Type? customTypeSerializer = null)
        if (data.AttributeClass.ToDisplayString().Contains(DataFieldAttributeName))
        {
            name = GetAttributeArgument(data, 0, "tag", name);
            readOnly = GetAttributeArgument(data, 1, "readOnly", readOnly);
            priority = GetAttributeArgument(data, 2, "priority", priority);
            isDataFieldAttribute = true;
            required = GetAttributeArgument(data, 3, "required", required);
            serverOnly = GetAttributeArgument(data, 4, "serverOnly", serverOnly);
        }
        // (int priority = 1, Type? customTypeSerializer = null)
        else if (data.AttributeClass.ToDisplayString().Contains(IdDataFieldAttributeName))
        {
            name = "id";
            priority = GetAttributeArgument(data, 0, "priority", priority);
            isDataFieldAttribute = true;
        }
        // (Type prototypeIdSerializer, int priority = 1)
        else if (data.AttributeClass.ToDisplayString().Contains(ParentDataFieldAttributeName))
        {
            name = "parent";
            priority = GetAttributeArgument(data, 1, "priority", priority);
            isDataFieldAttribute = true;
        }
        // (int priority = 1)
        else if (data.AttributeClass.ToDisplayString().Contains(AbstractDataFieldAttributeName))
        {
            name = "abstract";
            priority = GetAttributeArgument(data, 0, "priority", priority);
            isDataFieldAttribute = true;
        }
        // (bool readOnly = false, int priority = 1, bool serverOnly = false, Type? customTypeSerializer = null)
        else if (data.AttributeClass.ToDisplayString().Contains(IncludeDataFieldAttributeName))
        {
            var span = fieldName.AsSpan();
            name = $"{char.ToLowerInvariant(span[0])}{span.Slice(1).ToString()}";
            readOnly = GetAttributeArgument(data, 0, "readOnly", readOnly);
            priority = GetAttributeArgument(data, 1, "priority", priority);
            include = true;
            serverOnly = GetAttributeArgument(data, 2, "serverOnly", serverOnly);
        }

        var camelCasedName = ToCamelCase(fieldName);
        if (string.IsNullOrWhiteSpace(name))
            name = ToCamelCase(camelCasedName);

        return new DataFieldAttribute(
            data,
            name!,
            readOnly,
            priority,
            include,
            isDataFieldAttribute,
            required,
            serverOnly,
            camelCasedName
        );
    }

    private static T GetAttributeArgument<T>(AttributeData data, int constructorIndex, string namedArgument, T defaultValue)
    {
        if (constructorIndex < data.ConstructorArguments.Length)
        {
            var argument = data.ConstructorArguments[constructorIndex];
            if (!argument.IsNull && argument.Value is T value)
                return value;
        }

        foreach (var named in data.NamedArguments)
        {
            if (named.Key == namedArgument && !named.Value.IsNull && named.Value.Value is T value)
                return value;
        }

        return defaultValue;
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

        foreach (var attr in attributes)
        {
            if (attr.AttributeClass is not { } attributeClass)
                continue;

            if (TypeSymbolHelper.Inherits(attributeClass, DataFieldBaseNamespace))
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
                var associatedProperty = field.AssociatedSymbol as IPropertySymbol;
                var isDataRecordBackingField = isDataRecord &&
                                               field.IsImplicitlyDeclared &&
                                               associatedProperty != null;
                var fieldName = isDataRecordBackingField
                    ? associatedProperty!.Name
                    : field.Name;

                if (IsDataField(field.GetAttributes(),
                        fieldName,
                        isDataRecordBackingField ? false : field.IsImplicitlyDeclared,
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
                var hasSetter = property.SetMethod != null;
                if (IsDataField(property.GetAttributes(),
                        property.Name,
                        property.IsImplicitlyDeclared,
                        property.IsStatic,
                        hasSetter,
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

    private static bool IsAutoProperty(IPropertySymbol property)
    {
        foreach (var reference in property.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is not PropertyDeclarationSyntax syntax)
                continue;

            if (syntax.ExpressionBody != null)
                return false;

            if (syntax.AccessorList == null)
                return false;

            return syntax.AccessorList.Accessors.All(accessor =>
                accessor.Body == null &&
                accessor.ExpressionBody == null &&
                accessor.SemicolonToken.RawKind != 0);
        }

        return false;
    }

    public static string ToCamelCase(string name)
    {
        if (name == "ID")
            return "id";

        var span = name.AsSpan();
        return $"{char.ToLowerInvariant(span[0])}{span.Slice(1).ToString()}";
    }
}
