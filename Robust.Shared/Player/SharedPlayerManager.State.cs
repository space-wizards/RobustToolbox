using System.Collections.Generic;
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

    public void GetPlayerStates(GameTick fromTick, List<SessionState> states)
    {
        states.Clear();
        if (LastStateUpdate < fromTick)
            return;

        // TODO PlayerManager delta states
        // Track last update tick/time per session, and only send sessions that actually changed.

        states.EnsureCapacity(InternalSessions.Count);
        foreach (var player in InternalSessions.Values)
        {
            states.Add(player.State);
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
