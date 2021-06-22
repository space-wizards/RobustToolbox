using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    class ClickableComponentState : ComponentState
    {
        public Box2? LocalBounds { get; }

        public ClickableComponentState(Box2? localBounds)
        {
            LocalBounds = localBounds;
        }
    }
}
