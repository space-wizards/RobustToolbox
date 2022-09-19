using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics.Components
{
    // TODO: Move to content
    [Serializable, NetSerializable]
    public enum BodyStatus: byte
    {
        OnGround,
        InAir
    }
}
