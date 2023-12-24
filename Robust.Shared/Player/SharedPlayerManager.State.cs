using System.Collections.Generic;
using System.Linq;
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

        // Integration tests need to clone data before "sending" it to the client. Otherwise they reference the
        // same object.

#if FULL_RELEASE
        states.AddRange(InternalSessions.Values.Select(s => s.State));
#else
        states.AddRange(InternalSessions.Values.Select(s => s.State.Clone()));
#endif
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
