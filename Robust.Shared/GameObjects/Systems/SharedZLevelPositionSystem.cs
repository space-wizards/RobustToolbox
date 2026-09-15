using Robust.Shared.Map.Components;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Handles continuous z-level render height.
/// </summary>
public abstract class SharedZLevelPositionSystem : EntitySystem
{
    public void SetLocalHeight(Entity<ZLevelPositionComponent?> entity, float height)
    {
        if (!Resolve(entity, ref entity.Comp) || !float.IsFinite(height) || entity.Comp.LocalHeight.Equals(height))
            return;

        var oldHeight = entity.Comp.LocalHeight;
        entity.Comp.LocalHeight = height;
        DirtyField(entity.Owner, entity.Comp, nameof(ZLevelPositionComponent.LocalHeight));

        OnLocalHeightChanged(entity.Owner, height);
        var ev = new ZLevelPositionChangedEvent(oldHeight, height, false);
        RaiseLocalEvent(entity.Owner, ref ev);
    }

    /// <summary>
    /// Marks a map change as part of continuous z motion so clients may interpolate it.
    /// </summary>
    public void MarkContinuousMapMove(Entity<ZLevelPositionComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        // The sequence tells clients this move came from z physics.
        entity.Comp.ContinuousMapMoveSequence++;
        DirtyField(entity.Owner, entity.Comp, nameof(ZLevelPositionComponent.ContinuousMapMoveSequence));
        var ev = new ZLevelPositionChangedEvent(
            entity.Comp.LocalHeight,
            entity.Comp.LocalHeight,
            true);
        RaiseLocalEvent(entity.Owner, ref ev);
    }

    protected virtual void OnLocalHeightChanged(EntityUid uid, float height)
    {
    }
}

/// <summary>
/// Raised on an entity after its local height changes or a continuous map move is marked.
/// </summary>
[ByRefEvent]
public readonly record struct ZLevelPositionChangedEvent(
    float OldHeight,
    float NewHeight,
    bool ContinuousMapMove);
