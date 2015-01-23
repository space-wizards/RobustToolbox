using System;

namespace SS14.Shared
{
    public class BoundKeyEventArgs : EventArgs
    {
        public BoundKeyFunctions Function;
        public BoundKeyState FunctionState;
        public float time;

        public BoundKeyEventArgs(BoundKeyState functionState, BoundKeyFunctions function)
        {
            FunctionState = functionState;
            Function = function;
        }
    }
}