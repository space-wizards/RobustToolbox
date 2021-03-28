using Robust.Shared.Log;
using System;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Client.State
{
    internal sealed class StateManager : IStateManager
    {
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;

        public event Action<StateChangedEventArgs>? OnStateChanged;
        public State CurrentState { get; private set; }

        public StateManager()
        {
            CurrentState = new DefaultState();
        }

        public void FrameUpdate(FrameEventArgs e)
        {
            CurrentState?.FrameUpdate(e);
        }

        public void RequestStateChange<T>() where T : State, new()
        {
            RequestStateChange(typeof(T));
        }

        public void RequestStateChange(Type type)
        {
            if(!typeof(State).IsAssignableFrom(type))
                throw new ArgumentException($"Needs to be derived from {typeof(State).FullName}", nameof(type));

            if (CurrentState?.GetType() != type)
            {
                SwitchToState(type);
            }
        }

        private void SwitchToState(Type type)
        {
            Logger.Debug($"Switching to state {type}");

            var newState = _typeFactory.CreateInstance<State>(type);

            var old = CurrentState;
            CurrentState?.Shutdown();

            CurrentState = newState;
            CurrentState.Startup();

            OnStateChanged?.Invoke(new StateChangedEventArgs(old, CurrentState));
        }
    }
}
