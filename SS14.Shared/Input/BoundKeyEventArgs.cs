using System;

namespace SS14.Shared.Input
{
    public class BoundKeyEventArgs : EventArgs
    {
        public BoundKeyFunction Function { get; }
        public BoundKeyState State { get; }

        public BoundKeyEventArgs(BoundKeyFunction function, BoundKeyState state)
        {
            Function = function;
            State = state;
        }
    }
}
