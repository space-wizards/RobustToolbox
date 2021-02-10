using System;

namespace Robust.Client.State
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public StateChangedEventArgs(Client.State.State oldState, Client.State.State newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public Client.State.State OldState { get; }
        public Client.State.State NewState { get; }
    }
}
