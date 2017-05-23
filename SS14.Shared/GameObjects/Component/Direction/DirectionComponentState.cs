using System;

namespace SS14.Shared.GameObjects.Components.Direction
{
    [Serializable]
    public class DirectionComponentState : ComponentState
    {
        public SS14.Shared.Direction Direction;

        public DirectionComponentState(SS14.Shared.Direction dir)
            :base(ComponentFamily.Direction)
        {
            Direction = dir;
        }
    }
}
