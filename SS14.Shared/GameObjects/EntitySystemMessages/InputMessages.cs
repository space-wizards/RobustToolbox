using System;
using SS14.Shared.Input;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class BoundKeyChangedMessage : EntitySystemMessage
    {
        public BoundKeyFunctions Function { get; }
        public BoundKeyState State { get; }

        public BoundKeyChangedMessage(BoundKeyFunctions function, BoundKeyState state)
        {
            Function = function;
            State = state;
        }
    }
}
