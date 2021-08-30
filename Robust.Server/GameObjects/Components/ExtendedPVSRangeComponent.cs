using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    public sealed class ExtendedPVSRangeComponent : Component
    {
        public Dictionary<IComponent, Box2?> Bounds { get; } = new();
        public override string Name => "ExtendedPVSRange";
    }
}
