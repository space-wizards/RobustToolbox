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

    public void GetPlayerStates(IList<SessionState> playerStates, GameTick fromTick)
    {
        if (LastStateUpdate < fromTick)
        {
            return;
        }

        Lock.EnterReadLock();
        try
        {
#if FULL_RELEASE
            foreach (var ses in InternalSessions.Values)
            {
                playerStates.Add(ses.State);
            }
#else
            // Integration tests need to clone data before "sending" it to the client. Otherwise they reference the
            // same object.
            foreach (var ses in InternalSessions.Values)
            {
                playerStates.Add(ses.State.Clone());
            }
#endif
        }
        finally
        {
            Lock.ExitReadLock();
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
