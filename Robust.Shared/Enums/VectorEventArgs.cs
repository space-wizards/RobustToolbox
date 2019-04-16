using System;
using Robust.Shared.Map;

namespace Robust.Shared.Enums
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
