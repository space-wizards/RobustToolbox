using System;
using SS14.Shared.Map;

namespace SS14.Shared.Enums
{
    public class MoveEventArgs : EventArgs
    {
        public MoveEventArgs(GridCoordinates oldPos, GridCoordinates newPos)
        {
            OldPosition = oldPos;
            NewPosition = newPos;
        }

        public GridCoordinates OldPosition { get; }
        public GridCoordinates NewPosition { get; }
    }
}
