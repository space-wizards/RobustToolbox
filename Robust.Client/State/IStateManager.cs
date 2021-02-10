using System;
using Robust.Shared.Timing;

namespace Robust.Client.State
{
    public interface IStateManager
    {
        event Action<StateChangedEventArgs> OnStateChanged;

        Client.State.State CurrentState { get; }
        void RequestStateChange<T>() where T : Client.State.State, new();
        void Update(FrameEventArgs e);
        void FrameUpdate(FrameEventArgs e);
        void FormResize();
        void RequestStateChange(Type type);
    }
}