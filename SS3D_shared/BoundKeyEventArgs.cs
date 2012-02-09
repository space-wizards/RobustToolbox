using System;

namespace SS13_Shared
{
    public class BoundKeyEventArgs : EventArgs
    {
        public BoundKeyEventArgs(BoundKeyState functionState, BoundKeyFunctions function)
        {
            FunctionState = functionState;
            Function = function;
	    }

        public BoundKeyState FunctionState;
        public BoundKeyFunctions Function;
        public float time;
    }
}
