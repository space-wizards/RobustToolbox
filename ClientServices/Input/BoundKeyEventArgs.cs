using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;

namespace ClientServices.Input
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
