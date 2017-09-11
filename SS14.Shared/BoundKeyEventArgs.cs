using System;

namespace SS14.Shared
{
    public class BoundKeyEventArgs : EventArgs
    {
        public BoundKeyFunctions Function { get; set; }
        public BoundKeyState FunctionState { get; set; }
        public float time { get; set; }

        public BoundKeyEventArgs(BoundKeyState functionState, BoundKeyFunctions function)
        {
            FunctionState = functionState;
            Function = function;
        }
    }
}