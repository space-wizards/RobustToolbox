using System;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
/// This struct is just a wrapper around a <see cref="NetEntity"/> that is intended to be used to store references to
/// entities in a context where there is no expectation that the entity still exists (has not been deleted).
/// </summary>
/// <remarks>
/// The current convention is that a non-null EntityUid stored on a component should correspond to an
/// existing entity. Generally, if such an entity has since been deleted or had a relevant component removed, the
/// references to that entity should have been cleaned up by the component shutdown logic. If this is not done, this
/// generally results in errors being logged when an invalid EntityUid is passed around. This struct exists to for
/// cases where you want to store an entity reference, while making it clear that there is no expectation that it
/// should continue to be valid, which also means you do not need to clean up any references upon deletion or
/// component removal.
/// </remarks>
/// <remarks>
/// When saving a map, any weak references to entities that are not being included in the save file are automatically
/// ignored.
/// </remarks>
[CopyByRef, Serializable, NetSerializable]
public record struct WeakEntityReference(NetEntity Entity)
{
    public override int GetHashCode() => Entity.GetHashCode();
    public static readonly WeakEntityReference Invalid = new(NetEntity.Invalid);

    public static WeakEntityReference Parse(ReadOnlySpan<char> uid) => new(NetEntity.Parse(uid));

    public static bool TryParse(ReadOnlySpan<char> uid, out WeakEntityReference entity)
    {
        if (NetEntity.TryParse(uid, out var nent))
        {
            entity = new(nent);
            return true;
        }

        entity = Invalid;
        return false;
    }
}

/// <summary>
/// Variant of <see cref="WeakEntityReference"/> that is only considered valid if the entity exists and still has the
/// specified component.
/// </summary>
[CopyByRef, Serializable]
public record struct WeakEntityReference<T>(NetEntity Entity) where T : IComponent
{
    public override int GetHashCode() => Entity.GetHashCode();
    public static readonly WeakEntityReference<T> Invalid = new(NetEntity.Invalid);

    public static WeakEntityReference<T> Parse(ReadOnlySpan<char> uid) => new(NetEntity.Parse(uid));

    public static bool TryParse(ReadOnlySpan<char> uid, out WeakEntityReference<T> entity)
    {
        if (NetEntity.TryParse(uid, out var nent))
        {
            entity = new(nent);
            return true;
        }

        entity = Invalid;
        return false;
    }
}
