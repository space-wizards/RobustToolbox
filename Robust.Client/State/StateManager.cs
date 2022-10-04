using System;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client.State
{
    internal sealed class StateManager : IStateManager
    {
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
        [Dependency] private readonly IUserInterfaceManager _interfaceManager = default!;
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

        public T RequestStateChange<T>() where T : State, new()
        {
            return (T) RequestStateChange(typeof(T));
        }

        public State RequestStateChange(Type type)
        {
            if(!typeof(State).IsAssignableFrom(type))
                throw new ArgumentException($"Needs to be derived from {typeof(State).FullName}", nameof(type));

            return CurrentState?.GetType() == type ? CurrentState : SwitchToState(type);
        }

        private State SwitchToState(Type type)
        {
            Logger.Debug($"Switching to state {type}");

            var newState = _typeFactory.CreateInstance<State>(type);

            var old = CurrentState;
            CurrentState?.ShutdownInternal(_interfaceManager);

            CurrentState = newState;
            CurrentState.StartupInternal(_interfaceManager);

            OnStateChanged?.Invoke(new StateChangedEventArgs(old, CurrentState));

            return CurrentState;
        }
    }
}
