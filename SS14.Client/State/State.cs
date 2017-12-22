using System;
using System.Collections.Generic;
using SS14.Client.Input;
//using SS14.Client.Interfaces.Input;
//using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
//using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
//using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Client.Interfaces.ResourceManagement;

namespace SS14.Client.State
{
    public abstract class State
    {
        protected readonly IConfigurationManager ConfigurationManager;
        //protected readonly IKeyBindingManager KeyBindingManager;
        protected readonly IMapManager MapManager;
        protected readonly IClientNetManager NetworkManager;
        //protected readonly IPlacementManager PlacementManager;
        protected readonly IPlayerManager PlayerManager;
        protected readonly IResourceCache ResourceCache;
        protected readonly IStateManager StateManager;
        //protected readonly IUserInterfaceManager UserInterfaceManager;

        /// <summary>
        ///     Constructs an instance of a screen.
        /// </summary>
        protected State(IDictionary<Type, object> managers)
        {
            StateManager = (IStateManager)managers[typeof(IStateManager)];
            NetworkManager = (IClientNetManager)managers[typeof(IClientNetManager)];
            ResourceCache = (IResourceCache)managers[typeof(IResourceCache)];
            //UserInterfaceManager = (IUserInterfaceManager)managers[typeof(IUserInterfaceManager)];
            MapManager = (IMapManager)managers[typeof(IMapManager)];
            PlayerManager = (IPlayerManager)managers[typeof(IPlayerManager)];
            ConfigurationManager = (IConfigurationManager)managers[typeof(IConfigurationManager)];
            //PlacementManager = (IPlacementManager)managers[typeof(IPlacementManager)];
            //KeyBindingManager = (IKeyBindingManager)managers[typeof(IKeyBindingManager)];
        }

        /// <summary>
        ///     Called the first time this state is constructed. It is called automatically. You should build your
        ///     UI elements in here.
        /// </summary>
        public abstract void InitializeGUI();

        /// <summary>
        ///     Screen is being (re)enabled.
        /// </summary>
        public abstract void Startup();

        /// <summary>
        ///     Screen is being disabled (NOT Destroyed).
        /// </summary>
        public abstract void Shutdown();

        /// <summary>
        ///     Update the contents of this screen.
        /// </summary>
        public virtual void Update(FrameEventArgs e) { }

        #region Events

        /// <summary>
        ///     Key was pressed.
        /// </summary>
        public virtual void KeyDown(KeyEventArgs e) { }

        /// <summary>
        ///     Key was released.
        /// </summary>
        public virtual void KeyUp(KeyEventArgs e) { }

        /// <summary>
        ///     Key was is STILL held.
        /// </summary>
        public virtual void KeyHeld(KeyEventArgs e) { }

        /// <summary>
        ///     Mouse button was pressed.
        /// </summary>
        public virtual void MousePressed(MouseButtonEventArgs e) { }

        /// <summary>
        ///     Mouse button was released.
        /// </summary>
        public virtual void MouseUp(MouseButtonEventArgs e) { }

        /// <summary>
        ///     Mouse button will be pressed.
        /// </summary>
        public virtual void MouseDown(MouseButtonEventArgs e) { }

        /// <summary>
        ///     Mouse cursor has moved.
        /// </summary>
        public virtual void MouseMoved(MouseMoveEventArgs e) { }

        /// <summary>
        ///     Mouse cursor will move.
        /// </summary>
        public virtual void MouseMove(MouseMoveEventArgs e) { }

        /// <summary>
        ///     Mouse wheel has been moved.
        /// </summary>
        public virtual void MouseWheelMove(MouseWheelEventArgs e) { }

        /// <summary>
        ///     Mouse has entered this screen.
        /// </summary>
        public virtual void MouseEntered(EventArgs e) { }

        /// <summary>
        ///     Left mouse button has been pressed.
        /// </summary>
        public virtual void MouseLeft(EventArgs e) { }

        /// <summary>
        ///     The screen has changed size, usually from resizing window. This is called automatically right after Startup.
        /// </summary>
        public virtual void FormResize() { }

        #endregion Events
    }
}
