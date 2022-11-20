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
    // figure out a way to just save NetMessage messages to replays doing this bullish to use IRobustSerializer as a
    // crutch seems shitty.
    [Serializable, NetSerializable]
    public sealed class CvarChangeMsg
    {
        public List<(string name, object value)> ReplicatedCvars = default!;
    }
}
