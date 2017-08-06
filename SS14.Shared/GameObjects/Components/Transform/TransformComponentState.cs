using System;
using SFML.System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class TransformComponentState : ComponentState
    {
        public readonly bool ForceUpdate;
        public readonly Vector2f Position;

        public TransformComponentState(Vector2f position, bool forceUpdate)
            : base(NetIDs.TRANSFORM)
        {
            Position = position;
            ForceUpdate = forceUpdate;
        }
    }
}
