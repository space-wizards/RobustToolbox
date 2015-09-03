using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SFML.Window;
using SS14.Client.Graphics.Event;
using KeyEventArgs = SFML.Window.KeyEventArgs;

namespace SS14.Client.Services.State
{
    public class StateManager : IStateManager
    {
        private readonly Dictionary<Type, IState> _loadedStates;
        private readonly Dictionary<Type, object> _managers;

        #region IStateManager Members

        public IState CurrentState { get; private set; }

        #endregion

        #region Constructor

        public StateManager(IPlayerConfigurationManager configurationManager, INetworkManager networkManager,
                            IUserInterfaceManager userInterfaceManager,
                            IResourceManager resourceManager, IMapManager mapManager, IPlayerManager playerManager,
                            IPlacementManager placementManager, IKeyBindingManager keyBindingManager)
        {
            _managers = new Dictionary<Type, object>
                            {
                                {typeof (INetworkManager), networkManager},
                                {typeof (IUserInterfaceManager), userInterfaceManager},
                                {typeof (IResourceManager), resourceManager},
                                {typeof (IMapManager), mapManager},
                                {typeof (IPlayerManager), playerManager},
                                {typeof (IPlayerConfigurationManager), configurationManager},
                                {typeof (IPlacementManager), placementManager},
                                {typeof (IKeyBindingManager), keyBindingManager},
                                {typeof (IStateManager), this}
                            };

            _loadedStates = new Dictionary<Type, IState>();
            CurrentState = null;

            playerManager.RequestedStateSwitch += HandleStateChange;
        }

        #endregion

        #region Input

        public void KeyDown(KeyEventArgs e)
        {
            if (CurrentState != null)
                CurrentState.KeyDown(e);
        }

        public void KeyUp(KeyEventArgs e)
        {
            if (CurrentState != null)
                CurrentState.KeyUp(e);
        }

        public void MouseUp(MouseButtonEventArgs e)
        {
            if (CurrentState != null)
                CurrentState.MouseUp(e);
        }

        public void MouseDown(MouseButtonEventArgs e)
        {
            if (CurrentState != null)
                CurrentState.MouseDown(e);
        }

        public void MouseMove(MouseMoveEventArgs e)
        {
            if (CurrentState != null)
                CurrentState.MouseMove(e);
        }

        public void MouseWheelMove(MouseWheelEventArgs e)
        {
            if (CurrentState != null)
                CurrentState.MouseWheelMove(e);
        }

        public void MouseEntered(EventArgs e)
        {
            if (CurrentState != null)
                CurrentState.MouseEntered(e);
        }

        public void MouseLeft(EventArgs e)
        {
            if (CurrentState != null)
                CurrentState.MouseLeft(e);
        }

        #endregion

        #region Updates & Statechanges

        public void Update(FrameEventArgs e)
        {
            if (CurrentState == null) return;

            CurrentState.Update(e);
            CurrentState.Render(e);
        }

        public void RequestStateChange<T>() where T : IState
        {
            if (CurrentState == null || CurrentState.GetType() != typeof (T))
                SwitchToState<T>();
        }

        public void FormResize()
        {
            if (CurrentState == null)
                return;

            CurrentState.FormResize();
        }

        private void SwitchToState<T>() where T : IState
        {
            IState newState;

            if (_loadedStates.ContainsKey(typeof (T)))
            {
                newState = (T) _loadedStates[typeof (T)];
            }
            else
            {
                var parameters = new object[] {_managers};
                newState = (T) Activator.CreateInstance(typeof (T), parameters);
                _loadedStates.Add(typeof (T), newState);
            }

            if (CurrentState != null) CurrentState.Shutdown();

            CurrentState = newState;
            CurrentState.Startup();
        }

        private void RequestStateChange(Type type)
        {
            if (CurrentState.GetType() != type)
            {
                SwitchToState(type);
            }
        }

        private void SwitchToState(Type type)
        {
            IState newState;

            if (_loadedStates.ContainsKey(type))
            {
                newState = _loadedStates[type];
            }
            else
            {
                var parameters = new object[] {_managers};
                newState = (IState) Activator.CreateInstance(type, parameters);
                _loadedStates.Add(type, newState);
            }

            if (CurrentState != null) CurrentState.Shutdown();

            CurrentState = newState;
            CurrentState.Startup();
        }

        private void HandleStateChange(object sender, TypeEventArgs args)
        {
            if (args.Type.GetInterface("IState") != null)
            {
                RequestStateChange(args.Type);
            }
        }

        #endregion
    }
}