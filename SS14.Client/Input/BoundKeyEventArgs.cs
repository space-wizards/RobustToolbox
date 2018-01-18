using System;
using SS14.Shared;
using SS14.Shared.Enums;

namespace SS14.Client.Input
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