using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Physics.Events;

/// <summary>
///     Directed event raised when an entity's physics BodyType changes.
/// </summary>
[ByRefEvent]
public readonly struct PhysicsBodyTypeChangedEvent
{
    public readonly EntityUid Entity;

    /// <summary>
    ///     New BodyType of the entity.
    /// </summary>
    public readonly BodyType New;

    /// <summary>
    ///     Old BodyType of the entity.
    /// </summary>
    public readonly BodyType Old;

    public readonly PhysicsComponent Component;

    public PhysicsBodyTypeChangedEvent(EntityUid entity, BodyType newType, BodyType oldType, PhysicsComponent component)
    {
        Entity = entity;
        New = newType;
        Old = oldType;
        Component = component;
    }
}