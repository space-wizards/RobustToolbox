namespace Robust.Shared.GameObjects;

/// <summary>
/// Raised directed at an entity when <see cref="TransformComponent.GridUid"/> is modified.
/// This event is only raised if the entity has the <see cref="MetaDataFlags.ExtraTransformEvents"/> flag set.
/// </summary>
/// <remarks>
/// Event handlers should not modify positions or delete the entity, because the move event that triggered this event
/// is still being processed. This may also mean that the entity's current position/parent has not yet been updated,
/// and that positional entity queries are not reliable.
/// </remarks>
[ByRefEvent]
public readonly record struct GridUidChangedEvent(
    Entity<TransformComponent, MetaDataComponent> Entity,
    EntityUid? OldGrid)
{
    public EntityUid? NewGrid => Entity.Comp1.GridUid;
    public EntityUid Uid => Entity.Owner;
    public TransformComponent Transform => Entity.Comp1;
    public MetaDataComponent Meta => Entity.Comp2;
}
