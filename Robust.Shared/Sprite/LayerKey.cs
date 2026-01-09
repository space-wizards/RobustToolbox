using System;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Sprite;

/// <summary>
/// This struct represents a key used to reference a sprite layer. The actual underlying key may be either an enum or
/// a string.
/// </summary>
[Serializable, NetSerializable, CopyByRef]
public readonly record struct LayerKey
{
    public readonly string? StringKey;
    public readonly Enum? EnumKey;
    public static LayerKey Invalid => default;
    public bool IsValid => StringKey != null || EnumKey != null;

    public LayerKey(string key)
    {
        StringKey = key;
    }

    public LayerKey(Enum key)
    {
        EnumKey = key;
    }

    public static implicit operator LayerKey(string key)
    {
        return new(key);
    }

    public static implicit operator LayerKey(Enum key)
    {
        return new(key);
    }

    public static LayerKey Parse(IReflectionManager refMan, string raw)
    {
        if (TryParse(refMan, raw, out var key))
            return key;

        throw new ArgumentException($"{raw} is not a valid enum");
    }

    public static bool TryParse(IReflectionManager refMan, string raw, out LayerKey key)
    {
        if (!raw.StartsWith("enum."))
        {
            key = new (raw);
            return true;
        }

        if (refMan.TryParseEnumReference(raw, out var @enum))
        {
            key = new(@enum);
            return true;
        }

        key = default;
        return false;
    }
}
