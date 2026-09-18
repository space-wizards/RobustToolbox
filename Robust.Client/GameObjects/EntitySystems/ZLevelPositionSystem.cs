using System.Collections.Generic;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;

namespace Robust.Client.GameObjects;

/// <summary>
/// Adds replicated height changes to transform interpolation.
/// </summary>
public sealed partial class ZLevelPositionSystem : SharedZLevelPositionSystem
{
    private readonly Dictionary<EntityUid, PositionState> _states = new();

    [SubscribeLocalEvent]
    private void OnStartup(Entity<ZLevelPositionComponent> entity, ref ComponentStartup args)
    {
        _states[entity.Owner] = new PositionState(entity.Comp.LocalHeight, entity.Comp.ContinuousMapMoveSequence);
    }

    [SubscribeLocalEvent]
    private void OnPositionRemoved(Entity<ZLevelPositionComponent> entity, ref ComponentRemove args)
    {
        _states.Remove(entity.Owner);
    }

    [SubscribeLocalEvent]
    private void OnAfterState(Entity<ZLevelPositionComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        // A state can change the height, the move marker, or both.
        var old = _states.TryGetValue(entity.Owner, out var state)
            ? state
            : new PositionState(entity.Comp.LocalHeight, entity.Comp.ContinuousMapMoveSequence);
        var oldHeight = old.Height;
        var oldSequence = old.Sequence;
        var continuousMapMove = oldSequence != entity.Comp.ContinuousMapMoveSequence;
        _states[entity.Owner] = new PositionState(entity.Comp.LocalHeight, entity.Comp.ContinuousMapMoveSequence);
        if (oldHeight.Equals(entity.Comp.LocalHeight) && !continuousMapMove)
            return;

        var ev = new ZLevelPositionChangedEvent(oldHeight, entity.Comp.LocalHeight, continuousMapMove);
        RaiseLocalEvent(entity.Owner, ref ev);
    }

    protected override void OnLocalHeightChanged(EntityUid uid, float height)
    {
        if (_states.TryGetValue(uid, out var state))
            _states[uid] = state with { Height = height };
    }

    private readonly record struct PositionState(float Height, uint Sequence);
}
