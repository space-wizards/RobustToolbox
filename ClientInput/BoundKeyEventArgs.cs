using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientInput
{
    public class BoundKeyEventArgs : EventArgs
    {
      	public BoundKeyEventArgs(KeyState functionState, KeyFunctions function) {
            FunctionState = functionState;
            Function = function;
	    }

        public KeyState FunctionState;
        public KeyFunctions Function;
    }
}
