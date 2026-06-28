using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Robust.Shared.Collections;

namespace Robust.Shared.Serialization;

/// <summary>
/// Rejects serialized network payloads for this type above the specified byte size.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
public sealed class NetMaxSerializedSizeAttribute(int bytes) : Attribute
{
    public int Bytes { get; } = bytes;
}

/// <summary>
/// Rejects deserialized network field or property values above the specified length.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class NetMaxLengthAttribute(int length) : Attribute
{
    public int Length { get; } = length;
}

public interface INetValidationManager
{
    void RegisterTypes(IEnumerable<Type> types);
    void RegisterType<T>();
    void RegisterType(Type type);
    void ValidateSerializedSize<T>(int bytes);
    void ValidateSerializedSize(Type type, int bytes);
    void ValidateObject<T>(T? value);
}

public sealed class NetValidationManager : INetValidationManager
{
    private sealed record LengthMember(MemberInfo Member, int MaxLength);

    private sealed record ValidationData(LengthMember[] LengthMembers, int? SerializedSizeLimit);

    private FrozenDictionary<Type, ValidationData> _registeredTypes =
        new Dictionary<Type, ValidationData>().ToFrozenDictionary();

    public void RegisterTypes(IEnumerable<Type> types)
    {
        var registeredTypes = new Dictionary<Type, ValidationData>(_registeredTypes);

        foreach (var type in types)
        {
            registeredTypes.TryAdd(type, BuildValidationData(type));
        }

        _registeredTypes = registeredTypes.ToFrozenDictionary();
    }

    public void RegisterType<T>()
    {
        RegisterType(typeof(T));
    }

    public void RegisterType(Type type)
    {
        if (_registeredTypes.ContainsKey(type))
            return;

        var registeredTypes = new Dictionary<Type, ValidationData>(_registeredTypes)
        {
            [type] = BuildValidationData(type)
        };

        _registeredTypes = registeredTypes.ToFrozenDictionary();
    }

    public void ValidateSerializedSize<T>(int bytes)
    {
        ValidateSerializedSize(typeof(T), bytes);
    }

    public void ValidateSerializedSize(Type type, int bytes)
    {
        if (!_registeredTypes.TryGetValue(type, out var data) || data.SerializedSizeLimit is not { } max)
            return;

        if (bytes > max)
            throw new InvalidDataException($"{type.Name} serialized size {bytes} exceeds maximum {max}.");
    }

    public void ValidateObject<T>(T? value)
    {
        if (value == null)
            return;

        var type = value.GetType();
        if (!_registeredTypes.TryGetValue(type, out var data))
            return;

        foreach (var member in data.LengthMembers)
        {
            ValidateLength(type, member.Member, GetMemberValue(member.Member, value), member.MaxLength);
        }
    }

    private static ValidationData BuildValidationData(Type type)
        => new(
            BuildLengthMembers(type),
            type.GetCustomAttribute<NetMaxSerializedSizeAttribute>(true)?.Bytes);

    private static LengthMember[] BuildLengthMembers(Type type)
    {
        var members = new ValueList<LengthMember>();

        foreach (var field in GetFields(type))
        {
            var attr = field.GetCustomAttribute<NetMaxLengthAttribute>(true);
            if (attr == null)
                continue;

            members.Add(new LengthMember(field, attr.Length));
        }

        foreach (var property in GetProperties(type))
        {
            var attr = property.GetCustomAttribute<NetMaxLengthAttribute>(true);
            if (attr == null || property.GetIndexParameters().Length != 0)
                continue;

            members.Add(new LengthMember(property, attr.Length));
        }

        return members.ToArray();
    }

    private static object? GetMemberValue(MemberInfo member, object value)
    {
        return member switch
        {
            FieldInfo field => field.GetValue(value),
            PropertyInfo property => property.GetValue(value),
            _ => throw new ArgumentOutOfRangeException(nameof(member))
        };
    }

    private static IEnumerable<FieldInfo> GetFields(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                yield return field;
            }
        }
    }

    private static IEnumerable<PropertyInfo> GetProperties(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            foreach (var property in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                yield return property;
            }
        }
    }

    private static void ValidateLength(Type owner, MemberInfo member, object? value, int maxLength)
    {
        var length = GetLength(owner, member.Name, value);

        if (length > maxLength)
            throw new InvalidDataException($"{owner.Name}.{member.Name} length {length} exceeds maximum {maxLength}.");
    }

    private static int? GetLength(Type owner, string member, object? value)
    {
        return value switch
        {
            null => null,
            string str => str.Length,
            Array array => array.Length,
            ICollection collection => collection.Count,
            _ => throw new InvalidDataException($"{owner.Name}.{member} has {nameof(NetMaxLengthAttribute)} but is not a supported length-bearing type.")
        };
    }
}
