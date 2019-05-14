using Robust.Client.Input;
using System;

namespace Robust.Client.Interfaces.State
{
    public interface IStateManager
    {
        event Action<StateChangedEventArgs> OnStateChanged;

        Client.State.State CurrentState { get; }
        void RequestStateChange<T>() where T : Client.State.State, new();
        void Update(ProcessFrameEventArgs e);
        void FrameUpdate(RenderFrameEventArgs e);
        void MouseUp(MouseButtonEventArgs e);
        void MouseDown(MouseButtonEventArgs e);
        void MouseMove(MouseMoveEventArgs e);
        void MouseWheelMove(MouseWheelEventArgs e);
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
