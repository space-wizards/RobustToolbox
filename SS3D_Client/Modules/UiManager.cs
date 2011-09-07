using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D.Modules.UI;
using SS3D_shared;
using Lidgren.Network;
using GorgonLibrary.InputDevices;

namespace SS3D.Modules
{
    /// <summary>
    ///  Manages UI Components. This includes input, rendering, updates and net messages.
    /// </summary>
    class UiManager
    {
        #region Singleton
        private static UiManager singleton;

        private UiManager() { }

        public static UiManager Singleton
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new UiManager();
                }
                return singleton;
            }
        } 
        #endregion

        /// <summary>
        ///  List of iGuiComponents. Components in this list will recieve input, updates and net messages.
        /// </summary>
        public List<IGuiComponent> Components = new List<IGuiComponent>();

        private PlayerController player;

        /// <summary>
        ///  Currently not in use.
        /// </summary>
        public void Initialize(PlayerController _player)
        {
            player = _player;
        }

        /// <summary>
        ///  Disposes all components and clears component list. Components need to implement their own Dispose method.
        /// </summary>
        public void DisposeAllComponents()
        {
            var componentsToDispose = Components;
            Components.Clear();
            foreach (IGuiComponent current in componentsToDispose)
                current.Dispose();
            componentsToDispose = null;
        }

        /// <summary>
        ///  Returns all components of given type.
        /// </summary>
        public IEnumerable<IGuiComponent> GetComponentsByType(Type type)
        {
            return  from IGuiComponent comp in Components
                    where comp.GetType() == type
                    select comp;
        }

        /// <summary>
        ///  Returns the first component with a matching Type.
        /// </summary>
        public IGuiComponent GetSingleComponentByType(Type componentType)
        {
            return (from IGuiComponent comp in Components
                    where comp.GetType() == componentType
                    select comp).FirstOrDefault();
        }

        /// <summary>
        ///  Returns the first component with a matching GuiComponentType.
        /// </summary>
        public IGuiComponent GetSingleComponentByGuiComponentType(GuiComponentType componentType)
        {
            return (from IGuiComponent comp in Components
                   where comp.componentClass == componentType
                   select comp).FirstOrDefault();
        }

        /// <summary>
        ///  Returns all components with matching GuiComponentType.
        /// </summary>
        public IEnumerable<IGuiComponent> GetComponentsByGuiComponentType(GuiComponentType componentType)
        {
            return  from IGuiComponent comp in Components
                    where comp.componentClass == componentType
                    select comp;
        }

        /// <summary>
        ///  Handles Net messages directed at the UI manager or components thereof. This must be called by the currently active state. See GameScreen.
        /// </summary>
        public void HandleNetMessage(NetIncomingMessage msg)
        {
            UiManagerMessage uiMsg = (UiManagerMessage) msg.ReadByte();
            switch (uiMsg)
            {
                case UiManagerMessage.ComponentMessage:
                    HandleComponentMessage(msg);
                    break;
            }
        }

        /// <summary>
        ///  Handles and reroutes Net messages directed at components.
        /// </summary>
        public void HandleComponentMessage(NetIncomingMessage msg)
        {
            GuiComponentType component = (GuiComponentType)msg.ReadByte();
            var targetComponents = GetComponentsByGuiComponentType(component);
            foreach(IGuiComponent current in targetComponents)
                current.HandleNetworkMessage(msg);
        }

        #region Update & Render
        //These methods are called directly from the main loop to allow for cross-state ui elements. (Console maybe)

        /// <summary>
        ///  Updates the logic of UI components.
        /// </summary>
        public void Update()
        {
            foreach (IGuiComponent component in Components)
                component.Update();
        }

        /// <summary>
        ///  Renders UI components to screen.
        /// </summary>
        public void Render()
        {
            var renderList = from IGuiComponent comp in Components
                             where comp.IsVisible()
                             orderby comp.zDepth ascending
                             select comp;

            foreach (IGuiComponent component in renderList)
                component.Render();
        } 
        #endregion

        #region Input
        //The game states have to feed the UI Input!!! This is to allow more flexibility. The default methods do this.
        //Maybe this should be handled in the main methods for input. But then the state wouldnt have power over
        //this and things like the chat might get difficult.
        //Other Notes: When a component returns true to an input event the loop stops and so only that control recieves the input.
        //             That way we don't have all components reacting to one event.

        /// <summary>
        ///  Handles MouseDown event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool MouseDown(MouseInputEventArgs e)
        {
            var inputList = from IGuiComponent comp in Components
                            where comp.RecieveInput
                            orderby comp.zDepth ascending
                            //orderby comp.Focus descending
                            orderby comp.IsVisible() descending //Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                            select comp;

            foreach (IGuiComponent current in inputList)
                if (current.MouseDown(e)) return true;

            return false;
        }

        /// <summary>
        ///  Handles MouseUp event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool MouseUp(MouseInputEventArgs e)
        {
            var inputList = from IGuiComponent comp in Components
                            where comp.RecieveInput
                            orderby comp.zDepth ascending
                            //orderby comp.Focus descending
                            orderby comp.IsVisible() descending //Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                            select comp;

            foreach (IGuiComponent current in inputList)
                if (current.MouseUp(e)) return true;

            return false;
        }

        /// <summary>
        ///  Handles MouseMove event. Sent to all visible components.
        /// </summary>
        public virtual void MouseMove(MouseInputEventArgs e)
        {
            var inputList = from IGuiComponent comp in Components
                            where comp.RecieveInput
                            orderby comp.zDepth ascending
                            select comp;

            foreach (IGuiComponent current in inputList)
                current.MouseMove(e);
        }

        /// <summary>
        ///  Handles KeyDown event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool KeyDown(KeyboardInputEventArgs e)
        {
            var inputList = from IGuiComponent comp in Components
                            where comp.RecieveInput
                            orderby comp.zDepth ascending
                            //orderby comp.Focus descending
                            orderby comp.IsVisible() descending //Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                            select comp;

            foreach (IGuiComponent current in inputList)
                if (current.KeyDown(e)) return true;

            return false;
        } 
        #endregion
    }
}
