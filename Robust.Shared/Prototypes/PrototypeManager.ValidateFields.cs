using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using BindingFlags = System.Reflection.BindingFlags;

namespace Robust.Shared.Prototypes;

public partial class PrototypeManager
{
    /// <inheritdoc/>
    public List<string> ValidateFields(Dictionary<Type, HashSet<string>> prototypes)
    {
        var errors = new List<string>();
        foreach (var type in _reflectionManager.FindAllTypes())
        {
            // TODO validate public static fields on abstract classes that have no implementations?
            if (!type.IsAbstract)
                ValidateType(type, errors, prototypes);
        }

        return errors;
    }

    /// <summary>
    /// Validate all fields defined on this type and all base types.
    /// </summary>
    private void ValidateType(Type type, List<string> errors, Dictionary<Type, HashSet<string>> prototypes)
    {
        object? instance = null;
        Type? baseType = type;

        var flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                    BindingFlags.DeclaredOnly;

        while (baseType != null)
        {
            foreach (var field in baseType.GetFields(flags))
            {
                ValidateField(field, type, ref instance, errors, prototypes);
            }

            // We need to get the fields on the base type separately in order to get the private fields
            baseType = baseType.BaseType;
        }
    }

    private void ValidateField(
        FieldInfo field,
        Type type,
        ref object? instance,
        List<string> errors,
        Dictionary<Type, HashSet<string>> prototypes)
    {
        // Is this even a prototype id related field?
        if (!TryGetFieldPrototype(field, out var proto, out var canBeNull, out var canBeEmpty))
            return;

        if (field.FieldType != typeof(string))
        {
            errors.Add($"Prototype id field failed validation. Field is not a string. Field: {field.Name} in {type.FullName}");
            return;
        }

        if (!TryGetFieldValue(field, type, ref instance, errors, out var value))
            return;

        if (value == null)
        {
            if (!canBeNull)
                errors.Add($"Prototype id field failed validation. Const/Static fields should not be null. Field: {field.Name} in {type.FullName}");
            return;
        }

        var id = (string) value;

        if (string.IsNullOrWhiteSpace(id))
        {
            if (!canBeEmpty)
                errors.Add($"Prototype id field failed validation. Non-optional non-nullable data-fields must have a default value. Field: {field.Name} in {type.FullName}");
            return;
        }

        if (!prototypes.TryGetValue(proto, out var ids))
        {
            errors.Add($"Prototype id field failed validation. Unknown prototype kind. Field: {field.Name} in {type.FullName}");
            return;
        }

        if (!ids.Contains(id))
        {
            errors.Add($"Prototype id field failed validation. Unknown prototype: {id}. Field: {field.Name} in {type.FullName}");
        }
    }

    /// <summary>
    /// Get the value of some field. If this is not a static field, this will create instance of the object in order to
    /// validate default field values.
    /// </summary>
    private bool TryGetFieldValue(FieldInfo field, Type type, ref object? instance, List<string> errors, out object? value)
    {
        value = null;

        if (field.IsStatic || instance != null)
        {
            value = field.GetValue(instance);
            return true;
        }

        var constructor = type.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            Type.EmptyTypes);

        // TODO handle parameterless record constructors.
        // Figure out how ISerializationManager does it, or just re-use that code somehow.
        // In the meantime, record data fields need an explicit parameterless ctor.

        if (constructor == null)
        {
            errors.Add($"Prototype id field failed validation. Could not create instance to validate default value. Field: {field.Name} in {type.FullName}");
            return false;
        }

        instance = constructor.Invoke(Array.Empty<object>());
        value = field.GetValue(instance);

        return true;
    }

    private bool TryGetFieldPrototype(
        FieldInfo field,
        [NotNullWhen(true)] out Type? proto,
        out bool canBeNull,
        out bool canBeEmpty)
    {
        proto = null;
        canBeNull = false;
        canBeEmpty = false;

        // Check for a [PrototypeId] attribute.
        var attrib = field.GetCustomAttribute(typeof(ValidatePrototypeIdAttribute<>), false);
        if (attrib != null)
        {
            proto = attrib.GetType().GetGenericArguments().First();
            return true;
        }

        // Next, check for a data field attribute.
        if (!field.TryGetCustomAttribute(out DataFieldAttribute? dataField))
            return false;

        DebugTools.Assert(!field.IsStatic);

        if (dataField.CustomTypeSerializer == null)
            return false;

        // Check that this is a prototype id serializer
        if (!dataField.CustomTypeSerializer.IsGenericType)
            return false;

        if (dataField.CustomTypeSerializer.GetGenericTypeDefinition() != typeof(PrototypeIdSerializer<>))
            return false;

        proto = dataField.CustomTypeSerializer.GetGenericArguments().First();
        canBeEmpty = dataField.Required;

        // We will assume null values imply that the field itself is marked as nullable.
        // Unless someone can tell me how to figure out the nullability of a string field.
        canBeNull = true;

        return true;
    }
}
