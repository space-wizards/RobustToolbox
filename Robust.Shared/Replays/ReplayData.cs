using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;

namespace Robust.Shared.Replays;

[Serializable, NetSerializable]
public sealed class ReplayMessage
{
    public List<object> Messages = default!;

    // TODO REPLAYS
    // Figure out a way to just directly save NetMessage objects to replays. This just uses IRobustSerializer as a crutch.

    [Serializable, NetSerializable]
    public sealed class CvarChangeMsg
    {
        public List<(string name, object value)> ReplicatedCvars = default!;
    }
}
