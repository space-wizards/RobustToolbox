using System;

namespace SS13_Shared
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