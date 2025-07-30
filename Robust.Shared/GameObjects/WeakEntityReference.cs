using System;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
/// This struct is just a wrapper around an <see cref="EntityUid"/> that is intended to be used to store references to
/// entities in a context where there is no expectation that the entity still exists (has not been deleted).
/// </summary>
/// <remarks>
/// The current convention is that a non-null EntityUid stored on a component should correspond to an
/// existing entity. Generally, if such an entity has since been deleted or had a relevant component removed, the
/// references to that entity should have been cleaned up by the component shutdown logic. If this is not done, this
/// generally results in errors being logged when an invalid EntityUid is passed around. This struct exists for
/// cases where you want to store an entity reference, while making it clear that there is no expectation that it
/// should continue to be valid, which also means you do not need to clean up any references upon deletion or
/// component removal.
/// </remarks>
/// <remarks>
/// When saving a map, any weak references to entities that are not being included in the save file are automatically
/// ignored.
/// </remarks>
[CopyByRef, Serializable, NetSerializable]
public readonly record struct WeakEntityReference
{
    public readonly int Id;

    public readonly EntityUid Entity => new(Id);
    public WeakEntityReference(EntityUid uid)
    {
        Id = uid.Id;
    }
    public override int GetHashCode() => Id;
    public static readonly WeakEntityReference Invalid = new(EntityUid.Invalid);

    public static WeakEntityReference Parse(ReadOnlySpan<char> uid) => new(EntityUid.Parse(uid));

    public static bool TryParse(ReadOnlySpan<char> uid, out WeakEntityReference entity)
    {
        if (EntityUid.TryParse(uid, out var ent))
        {
            entity = new(ent);
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
public record struct WeakEntityReference<T>(EntityUid Entity) where T : IComponent
{
    public override int GetHashCode() => Entity.GetHashCode();
    public static readonly WeakEntityReference<T> Invalid = new(EntityUid.Invalid);

    public static WeakEntityReference<T> Parse(ReadOnlySpan<char> uid) => new(EntityUid.Parse(uid));

    public static bool TryParse(ReadOnlySpan<char> uid, out WeakEntityReference<T> entity)
    {
        if (EntityUid.TryParse(uid, out var ent))
        {
            entity = new(ent);
            return true;
        }

        entity = Invalid;
        return false;
    }
}
