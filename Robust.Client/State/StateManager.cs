using Robust.Client.Input;
using Robust.Client.Interfaces.State;
using Robust.Shared.Log;
using System;
using Robust.Shared.IoC;

namespace Robust.Client.State
{
    internal sealed class StateManager : IStateManager
    {
        [Dependency] private readonly IDynamicTypeFactory _typeFactory;

        public event Action<StateChangedEventArgs> OnStateChanged;
        public State CurrentState { get; private set; }

        #region Updates & Statechanges

        public void Update(ProcessFrameEventArgs e)
        {
            CurrentState?.Update(e);
        }

        public void FrameUpdate(RenderFrameEventArgs e)
        {
            CurrentState?.FrameUpdate(e);
        }

        public void FormResize()
        {
            CurrentState?.FormResize();
        }

        public void RequestStateChange<T>() where T : State, new()
        {
            RequestStateChange(typeof(T));
        }

        private void RequestStateChange(Type type)
        {
            if (CurrentState?.GetType() != type)
            {
                SwitchToState(type);
            }
        }

        private void SwitchToState(Type type)
        {
            Logger.Debug($"Switching to state {type}");

            var newState = (State)_typeFactory.CreateInstance(type);

            var old = CurrentState;
            CurrentState?.Shutdown();

            CurrentState = newState;
            CurrentState.Startup();

            OnStateChanged?.Invoke(new StateChangedEventArgs(old, CurrentState));
        }

        #endregion Updates & Statechanges
        #region Input

        public void MouseUp(MouseButtonEventArgs e)
        {
            CurrentState?.MouseUp(e);
        }

        public void MouseDown(MouseButtonEventArgs e)
        {
            CurrentState?.MouseDown(e);
        }

        public void MouseMove(MouseMoveEventArgs e)
        {
            CurrentState?.MouseMove(e);
        }

        public void MouseWheelMove(MouseWheelEventArgs e)
        {
            CurrentState?.MouseWheelMove(e);
        }

        public void MouseEntered(EventArgs e)
        {
            CurrentState?.MouseEntered(e);
        }

        public void MouseLeft(EventArgs e)
        {
            CurrentState?.MouseLeft(e);
        }

        #endregion Input
    }
}
