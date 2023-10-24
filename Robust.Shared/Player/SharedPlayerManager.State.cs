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

    public List<SessionState>? GetPlayerStates(GameTick fromTick)
    {
        if (LastStateUpdate < fromTick)
        {
            return null;
        }

        Lock.EnterReadLock();
        try
        {
#if FULL_RELEASE
                return InternalSessions.Values
                    .Select(s => s.State)
                    .ToList();
#else
            // Integration tests need to clone data before "sending" it to the client. Otherwise they reference the
            // same object.
            return InternalSessions.Values
                .Select(s => s.State.Clone())
                .ToList();
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
