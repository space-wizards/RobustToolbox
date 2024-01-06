using System;
using System.Collections.Generic;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Player;

internal sealed class CommonSession : ICommonSession
{
    [ViewVariables]
    public EntityUid? AttachedEntity { get; set; }

    [ViewVariables]
    public NetUserId UserId { get; }

    [ViewVariables]
    public string Name { get; internal set; } = "<Unknown>";

    [ViewVariables]
    public short Ping { get; internal set; }

    [ViewVariables]
    public DateTime ConnectedTime { get; set; }

    [ViewVariables]
    public SessionState State { get; } = new();

    [ViewVariables]
    public SessionStatus Status { get; set; } = SessionStatus.Connecting;

    [ViewVariables]
    public SessionData Data { get; }

    public bool ClientSide { get; set; }

    [ViewVariables]
    public INetChannel Channel { get; set; } = default!;

    [ViewVariables]
    public HashSet<EntityUid> ViewSubscriptions { get; } = new();

    [ViewVariables]
    public int VisibilityMask { get; set; } = 1;

    [ViewVariables]
    public LoginType AuthType => Channel?.AuthType ?? default;

    public override string ToString() => Name;

    public CommonSession(NetUserId user, string name, SessionData data)
    {
        UserId = user;
        Name = name;
        Data = data;
    }
}
