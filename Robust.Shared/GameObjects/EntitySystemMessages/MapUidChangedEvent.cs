using Robust.Shared.Map;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Raised directed at an entity when <see cref="TransformComponent.MapUid"/> is modified.
/// This event is only raised if the entity has the <see cref="MetaDataFlags.ExtraTransformEvents"/> flag set.
/// </summary>
/// <remarks>
/// Event handlers should not modify positions or delete the entity, because the move event that triggered this event
/// is still being processed. This may also mean that the entity's current position/parent has not yet been updated,
/// and that positional entity queries are not reliable.
/// </remarks>
[ByRefEvent]
public readonly record struct MapUidChangedEvent(
    Entity<TransformComponent, MetaDataComponent> Entity,
    EntityUid? OldMap,
    MapId OldMapId)
{
    public EntityUid? NewMap => Entity.Comp1.MapUid;
    public MapId? NewMapId => Entity.Comp1.MapID;
    public EntityUid Uid => Entity.Owner;
    public TransformComponent Transform => Entity.Comp1;
    public MetaDataComponent Meta => Entity.Comp2;
}
