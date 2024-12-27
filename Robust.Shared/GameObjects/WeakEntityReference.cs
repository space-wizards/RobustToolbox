using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

/// <summary>
/// This struct is just a wrapper around a <see cref="EntityUid"/> that is intended to be used to store references to
/// entities in a context where there is no expectation that the entity still exists (has not been deleted).
/// </summary>
/// <remarks>
/// The current convention is that a a non-null nullable EntityUid stored on a component should correspond to an
/// existing entity. Generally, if such an entity has since been deleted or had a relevant component removed, this
/// can result in errors being logged. This struct exists to for cases where you want to store an entity reference,
/// while making it clear that there is no expectation that it should continue to be valid, which also means you do not
/// need to clean up any references upon deletion or component removal.
/// </remarks>
/// <remarks>
/// When saving a map, any weak references to entities that are not being included in the save file are automatically
/// ignored.
/// </remarks>
[CopyByRef]
public record struct WeakEntityReference
{
    // Internal to dissuade anyone from accessing the field directly.
    // If made public to be more permissive for whatever reason, maybe add [Obsolete] do something else to generate a
    // warning to prevent accidental misuse?
    [ViewVariables] internal EntityUid Entity;
    public override int GetHashCode() => Entity.GetHashCode();
    public static readonly WeakEntityReference Invalid = new(EntityUid.Invalid);

    public WeakEntityReference(EntityUid uid)
    {
        Entity = uid;
    }
}

/// <summary>
/// Variant of <see cref="WeakEntityReference"/> that is only considered valid if the entity exists and still has the
/// specified component.
/// </summary>
[CopyByRef]
public record struct WeakEntityReference<T> where T : IComponent
{
    [ViewVariables] internal EntityUid Entity;
    public override int GetHashCode() => Entity.GetHashCode();
    public static readonly WeakEntityReference<T> Invalid = new(EntityUid.Invalid);

    public WeakEntityReference(EntityUid uid)
    {
        Entity = uid;
    }
}
