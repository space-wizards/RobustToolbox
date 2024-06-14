using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Robust.Shared.Player;

// This partial class has game-state related code.
internal abstract partial class SharedPlayerManager
{
    public void Dirty()
    {
        LastStateUpdate = Timing.CurTick;
    }

    public void GetPlayerStates(ICommonSession session, GameTick fromTick, List<SessionState> states)
    {
        states.Clear();
        if (LastStateUpdate < fromTick)
            return;

        states.EnsureCapacity(InternalSessions.Count);
        var ev = new GetSessionStateAttempt(session);
        EntManager.EventBus.RaiseEvent(EventSource.Local, ref ev);

        foreach (var player in InternalSessions.Values)
        {
            if (ev.Cancelled)
            {
                var copy = player.State.Clone();
                copy.Name = string.Empty;
                states.Add(copy);
            }
            else
            {
                states.Add(player.State);
            }
        }
    }

    public void UpdateState(ICommonSession session)
    {
        var state = session.State;
        state.UserId = session.UserId;
        state.Status = session.Status;
        state.Name = session.Name;
        state.ControlledEntity = EntManager.GetNetEntity(session.AttachedEntity);
        Dirty();
    }
}
