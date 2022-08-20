using System;
using Robust.Shared.Timing;

namespace Robust.Client.State
{
    public interface IStateManager
    {
        event Action<StateChangedEventArgs> OnStateChanged;

        State CurrentState { get; }
        T RequestStateChange<T>() where T : State, new();
        void FrameUpdate(FrameEventArgs e);
        State RequestStateChange(Type type);
    }
}
