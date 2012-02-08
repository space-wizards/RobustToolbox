using System;
using System.Collections.Generic;
using ClientInterfaces;
using ClientInterfaces.Input;
using ClientInterfaces.Network;
using ClientInterfaces.Placement;
using ClientInterfaces.Player;
using ClientInterfaces.State;
using ClientInterfaces.UserInterface;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS13_Shared;

namespace ClientServices.State
{
    /************************************************************************/
    /* state manager for program states                                     */
    /************************************************************************/

    public class StateManager : IStateManager
    {
        public IState CurrentState { get; private set; }
        private readonly Dictionary<Type, IState> _loadedStates;
        private readonly Dictionary<Type, object> _managers;

        #region Constructor

        public StateManager(IConfigurationManager configurationManager, INetworkManager networkManager, IUserInterfaceManager userInterfaceManager, 
            IResourceManager resourceManager, IMapManager mapManager, IPlayerManager playerManager, IPlacementManager placementManager, IKeyBindingManager keyBindingManager)
        {
            _managers = new Dictionary<Type, object>
                            {
                                {typeof (INetworkManager), networkManager},
                                {typeof (IUserInterfaceManager), userInterfaceManager},
                                {typeof (IResourceManager), resourceManager},
                                {typeof (IMapManager), mapManager},
                                {typeof (IPlayerManager), playerManager},
                                {typeof (IConfigurationManager), configurationManager},
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

        /// <summary>
        /// Gorgon method
        /// </summary>
        /// <param name="e"></param>
        public void KeyDown(KeyboardInputEventArgs e)
        {
            // if a state is active, call the states keydown method.
            if (CurrentState != null)
                CurrentState.KeyDown(e);
        }

        /// <summary>
        /// Gorgon method
        /// </summary>
        /// <param name="e"></param>
        public void KeyUp(KeyboardInputEventArgs e)
        {
            // if a state is active, call the states keyup method.
            if (CurrentState != null)
                CurrentState.KeyUp(e);
        }

        /// <summary>
        /// Gorgon method
        /// </summary>
        /// <param name="e"></param>
        public void MouseUp(MouseInputEventArgs e)
        {
            // if a state is active, call the states mouseup method.
            if (CurrentState != null)
                CurrentState.MouseUp(e);
        }

        /// <summary>
        /// Gorgon method
        /// </summary>
        /// <param name="e"></param>
        public void MouseDown(MouseInputEventArgs e)
        {
            // if a state is active, call the states mousedown method.
            if (CurrentState != null)
                CurrentState.MouseDown(e);
        }

        /// <summary>
        /// Gorgon method
        /// </summary>
        /// <param name="e"></param>
        public void MouseMove(MouseInputEventArgs e)
        {
            // if a state is active, call the states mousemove method.
            if (CurrentState != null)
                CurrentState.MouseMove(e);
        }

        public void MouseWheelMove(MouseInputEventArgs e)
        {
            if (CurrentState != null)
                CurrentState.MouseWheelMove(e);
        }

        #endregion

        #region Updates & Statechanges

        public void Update(FrameEventArgs e)
        {
            if (CurrentState == null) return;

            CurrentState.Update(e);
            CurrentState.GorgonRender(e);
        }

        public void RequestStateChange<T>() where T : IState
        {
            // don't change the state if the requested state class matches the current state
            if (CurrentState == null || CurrentState.GetType() != typeof(T))
                SwitchToState<T>();
        }

        private void SwitchToState<T>() where T : IState
        {
            IState newState;

            if (_loadedStates.ContainsKey(typeof(T)))
            {
                newState = (T)_loadedStates[typeof(T)];
            }
            else
            {
                var parameters = new object[] { _managers };
                newState = (T)Activator.CreateInstance(typeof(T), parameters);
                _loadedStates.Add(typeof(T), newState);
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
                var parameters = new object[] { _managers };
                newState = (IState)Activator.CreateInstance(type, parameters);
                _loadedStates.Add(type, newState);
            }

            if (CurrentState != null) CurrentState.Shutdown();

            CurrentState = newState;
            CurrentState.Startup();
        }

        private void HandleStateChange(object sender, TypeEventArgs args)
        {
            if ((args.Type as IState) != null)
            {
                RequestStateChange(args.Type);
            }
        }

        #endregion
    }
}