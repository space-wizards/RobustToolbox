using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using BindingFlags = System.Reflection.BindingFlags;

namespace Robust.Shared.Prototypes;

public partial class PrototypeManager
{
    /// <inheritdoc/>
    public List<string> ValidateStaticFields(Dictionary<Type, HashSet<string>> prototypes)
    {
        var errors = new List<string>();
        foreach (var type in _reflectionManager.FindAllTypes())
        {
            // TODO validate public static fields on abstract classes that have no implementations?
            if (!type.IsAbstract)
                ValidateStaticFieldsInternal(type, errors, prototypes);
        }

        return errors;
    }

    /// <inheritdoc/>
    public List<string> ValidateStaticFields(Type type, Dictionary<Type, HashSet<string>> prototypes)
    {
        var errors = new List<string>();
        ValidateStaticFieldsInternal(type, errors, prototypes);
        return errors;
    }

    /// <summary>
    /// Validate all static fields defined on this type and all base types.
    /// </summary>
    private void ValidateStaticFieldsInternal(Type type, List<string> errors, Dictionary<Type, HashSet<string>> prototypes)
    {
        var baseType = type;
        var flags = BindingFlags.Static |  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

        while (baseType != null)
        {
            foreach (var field in baseType.GetFields(flags))
            {
                DebugTools.Assert(field.IsStatic);
                ValidateStaticField(field, type, errors, prototypes);
            }

            // We need to get the fields on the base type separately in order to get the private fields
            baseType = baseType.BaseType;
        }
    }

    private void ValidateStaticField(
        FieldInfo field,
        Type type,
        List<string> errors,
        Dictionary<Type, HashSet<string>> prototypes)
    {
        DebugTools.Assert(field.IsStatic);
        DebugTools.Assert(!field.HasCustomAttribute<DataFieldAttribute>(), "Datafields should not be static");

        // Is this even a prototype id related field?
        if (!TryGetFieldPrototype(field, out var proto))
            return;

        if (!prototypes.TryGetValue(proto, out var validIds))
        {
            errors.Add($"Prototype id field failed validation. Unknown prototype kind {proto.Name}. Field: {field.Name} in {type.FullName}");
            return;
        }

        if (!TryGetIds(field, proto, out var ids))
        {
            TryGetIds(field, proto, out _);
            DebugTools.Assert($"Failed to get ids, despite resolving the field into a prototype kind?");
            return;
        }

        foreach (var id in ids)
        {
            if (!validIds.Contains(id))
                errors.Add($"Prototype id field failed validation. Unknown prototype: {id} of type {proto.Name}. Field: {field.Name} in {type.FullName}");
        }
    }

    /// <summary>
    /// Extract prototype ids from a string, IEnumerable{string}, EntProtoId, IEnumerable{EntProtoId}, ProtoId{T}, or IEnumerable{ProtoId{T}} field.
    /// </summary>
    private bool TryGetIds(FieldInfo field, Type proto, [NotNullWhen(true)] out string[]? ids)
    {
        ids = null;
        var value = field.GetValue(null);
        if (value == null)
            return false;

        if (value is string str)
        {
            ids = [str];
            return true;
        }

        if (value is IEnumerable<string> strEnum)
        {
            ids = strEnum.ToArray();
            return true;
        }

        if (value is EntProtoId protoId)
        {
            ids = [protoId];
            return true;
        }

        if (value is IEnumerable<EntProtoId> protoIdEnum)
        {
            ids = protoIdEnum.Select(x=> x.Id).ToArray();
            return true;
        }

        if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(ProtoId<>))
        {
            ids = [value.ToString()!];
            return true;
        }

        foreach (var iface in field.FieldType.GetInterfaces())
        {
            if (!iface.IsGenericType)
                continue;

            if (iface.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                continue;

            var enumType = iface.GetGenericArguments().Single();
            if (!enumType.IsGenericType)
                continue;

            if (enumType.GetGenericTypeDefinition() != typeof(ProtoId<>))
                continue;

            ids = GetIdsMethod.MakeGenericMethod(proto).Invoke(null, [value]) as string[];
            return ids != null;
        }

        return false;
    }

    private static MethodInfo GetIdsMethod = typeof(PrototypeManager).GetMethod(nameof(GetIds), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static string[] GetIds<T>(IEnumerable<ProtoId<T>> enumerable) where T : class, IPrototype
    {
        return enumerable.Select(x => x.Id).ToArray();
    }

    private bool TryGetFieldPrototype(FieldInfo field, [NotNullWhen(true)] out Type? proto)
    {
        // Validate anything with the attribute
        var attrib = field.GetCustomAttribute(typeof(ValidatePrototypeIdAttribute<>), false);
        if (attrib != null)
        {
            proto = attrib.GetType().GetGenericArguments().First();
            return true;
        }

        if (TryGetPrototypeFromType(field.FieldType, out proto))
            return true;

        // Allow validating arrays or lists.
        foreach (var iface in field.FieldType.GetInterfaces().Where(x => x.IsGenericType))
        {
            if (iface.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                continue;

            var enumType = iface.GetGenericArguments().Single();
            if (TryGetPrototypeFromType(enumType, out proto))
                return true;
        }

        proto = null;
        return false;
    }

    private bool TryGetPrototypeFromType(Type type, [NotNullWhen(true)] out Type? proto)
    {
        if (type == typeof(EntProtoId))
        {
            proto = typeof(EntityPrototype);
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ProtoId<>))
        {
            proto = type.GetGenericArguments().Single();
            DebugTools.Assert(proto != typeof(EntityPrototype), "Use EntProtoId instead of ProtoId<EntityPrototype>");
            return true;
        }

        proto = null;
        return false;
    }
}
