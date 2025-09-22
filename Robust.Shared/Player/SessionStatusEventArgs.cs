using System;
using Robust.Shared.Enums;

namespace Robust.Shared.Player;

public sealed class SessionStatusEventArgs : EventArgs
{
    public SessionStatusEventArgs(ICommonSession session, SessionStatus oldStatus, SessionStatus newStatus)
    {
        Session = session;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }

    public readonly ICommonSession Session;
    public readonly SessionStatus OldStatus;
    public readonly SessionStatus NewStatus;
}