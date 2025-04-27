namespace Robust.Shared.GameObjects;

/// <summary>
/// Raised when an entity's grid changes.
/// </summary>
[ByRefEvent]
public readonly record struct EntGridChangedEvent(
    EntityUid Entity,
    EntityUid? OldGrid,
    EntityUid? NewGrid,
    TransformComponent Transform)
{
    /// <summary>
    /// Entity whose grid has changed. The transform component has a property with the new grid.
    /// </summary>
    public readonly EntityUid Entity = Entity;

    /// <summary>
    /// Old grid that the entity was on.
    /// </summary>
    public readonly EntityUid? OldGrid = OldGrid;

    /// <summary>
    /// New grid that the entity is now on.
    /// </summary>
    public readonly EntityUid? NewGrid = NewGrid;

    /// <summary>
    /// The <see cref="TransformComponent" /> of <see cref="Entity" />.
    /// </summary>
    public readonly TransformComponent Transform = Transform;
}

