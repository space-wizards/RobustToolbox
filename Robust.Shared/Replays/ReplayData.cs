using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;

namespace Robust.Shared.Replays;

[Serializable, NetSerializable]
public sealed class ReplayMessage
{
    public List<object> Messages = default!;

    [Serializable, NetSerializable]
    public sealed class CvarChangeMsg
    {
        public List<(string name, object value)> ReplicatedCvars = default!;
        public (TimeSpan, GameTick) TimeBase = default;
    }
}
