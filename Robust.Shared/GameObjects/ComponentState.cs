using System;
using Robust.Shared.Analyzers;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [RequiresSerializable]
    [Serializable, NetSerializable]
    public class ComponentState { }
}
