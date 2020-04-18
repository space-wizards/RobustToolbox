using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class EntitySystemMessage : EntityEventArgs
    {
    }
}
