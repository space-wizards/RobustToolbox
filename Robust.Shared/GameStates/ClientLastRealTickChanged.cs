using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Robust.Shared.GameStates;

[Serializable, NetSerializable]
public sealed class ClientLastRealTickChanged(GameTick tick) : EntityEventArgs
{
    public readonly GameTick Tick = tick;
}
