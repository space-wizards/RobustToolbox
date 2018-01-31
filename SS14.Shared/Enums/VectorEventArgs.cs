using System;
using SS14.Shared.Map;

namespace SS14.Shared.Enums
{
    public class MoveEventArgs : EventArgs
    {
        public MoveEventArgs(LocalCoordinates oldPos, LocalCoordinates newPos)
        {
            OldPosition = oldPos;
            NewPosition = newPos;
        }

        public LocalCoordinates OldPosition { get; }
        public LocalCoordinates NewPosition { get; }
    }
}
