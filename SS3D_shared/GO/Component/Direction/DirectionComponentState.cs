using System;

namespace SS13_Shared.GO.Component.Direction
{
    [Serializable]
    public class DirectionComponentState : ComponentState
    {
        public SS13_Shared.Direction Direction;

        public DirectionComponentState(SS13_Shared.Direction dir)
            :base(ComponentFamily.Direction)
        {
            Direction = dir;
        }
    }
}