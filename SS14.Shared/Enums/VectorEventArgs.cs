using System;
using SS14.Shared.Map;

namespace SS14.Shared.Enums
{
    public class MoveEventArgs : EventArgs
    {
        public MoveEventArgs(GridLocalCoordinates oldPos, GridLocalCoordinates newPos)
        {
            OldPosition = oldPos;
            NewPosition = newPos;
        }

        public GridLocalCoordinates OldPosition { get; }
        public GridLocalCoordinates NewPosition { get; }
    }
}
