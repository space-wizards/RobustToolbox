using SFML.System;
using System;

namespace SS14.Shared.GameObjects.Components.Transform
{
    [Serializable]
    public class TransformComponentState : ComponentState
    {
        public bool ForceUpdate { get; set; }
        public Vector2f Position { get; set; }

        public TransformComponentState(Vector2f position, bool forceUpdate)
            : base(NetIDs.TRANSFORM)
        {
            Position = position;
            ForceUpdate = forceUpdate;
        }
    }
}
