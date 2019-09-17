using System;
using Robust.Shared.Timing;

namespace Robust.Client.Interfaces.State
{
    public interface IStateManager
    {
        event Action<StateChangedEventArgs> OnStateChanged;

        Client.State.State CurrentState { get; }
        void RequestStateChange<T>() where T : Client.State.State, new();
        void Update(FrameEventArgs e);
        void FrameUpdate(FrameEventArgs e);
        void FormResize();
    }

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
