using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class DirectionComponentState : ComponentState
    {
        public readonly Direction Direction;

        public DirectionComponentState(Direction dir)
            : base(NetIDs.DIRECTION)
        {
            Direction = dir;
        }
    }
}
