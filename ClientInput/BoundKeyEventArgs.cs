using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;

namespace ClientInput
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
    }
}
