using System;

namespace Robust.Client.State
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public StateChangedEventArgs(State oldState, State newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public State OldState { get; }
        public State NewState { get; }
    }
}
