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

    public void GetPlayerState(GameTick fromTick, ICommonSession? session, List<SessionState> states)
    {
        states.Clear();
        if (LastStateUpdate < fromTick)
            return;

        // TODO PlayerManager delta states
        // Track last update tick/time per session, and only send sessions that actually changed.
        states.EnsureCapacity(session == null || ConfigManager.GetCVar(CVars.NetShareAllClientSessions) ? InternalSessions.Count : 1);
        foreach (var player in InternalSessions.Values)
        {
            // Either we got the cvar on, or we're a replay, and therefore want all states...
            if (ConfigManager.GetCVar(CVars.NetShareAllClientSessions) || session == null)
            {
                states.Add(player.State);
            }
            // ... Or we only concern ourselves with the single session's state
            else if (session.UserId == player.UserId)
            {
                states.Add(player.State);
                return;
            }
        }
    }

    public void UpdateState(ICommonSession session)
    {
        var state = session.State;
        state.UserId = session.UserId;
        state.Status = session.Status;
        state.Name = session.Name;
        state.DisplayName = session.DisplayName;
        state.ControlledEntity = EntManager.GetNetEntity(session.AttachedEntity);
        Dirty();
    }
}
