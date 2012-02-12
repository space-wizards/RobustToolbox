using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using SS13_Shared;
using Lidgren.Network;
using GorgonLibrary.InputDevices;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using ClientInterfaces;

namespace ClientServices.UserInterface
{
    /// <summary>
    ///  Manages UI Components. This includes input, rendering, updates and net messages.
    /// </summary>
    public class UserInterfaceManager : IUserInterfaceManager
    {
        private IGuiComponent _currentFocus;

        private Vector2D _mousePos = Vector2D.Zero;
        private Sprite _cursorSprite;

        public IDragDropInfo DragInfo { get; private set; }

        /// <summary>
        ///  List of iGuiComponents. Components in this list will recieve input, updates and net messages.
        /// </summary>
        private readonly List<IGuiComponent> _components;

        private readonly IResourceManager _resourceManager;

        public UserInterfaceManager(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            DragInfo = new DragDropInfo();
            _components = new List<IGuiComponent>();
        }

        /// <summary>
        ///  Disposes all components and clears component list. Components need to implement their own Dispose method.
        /// </summary>
        public void DisposeAllComponents()
        {
            _components.ForEach(x => x.Dispose());
            _components.Clear();
        }

        /// <summary>
        ///  Disposes all components of the given type.
        /// </summary>
        public void DisposeAllComponents<T>()
        {
            var componentsOfType = (from IGuiComponent component in _components
                                    where component.GetType() == typeof (T)
                                    select component).ToList();

            foreach (var current in componentsOfType)
            {
                current.Dispose();
                _components.Remove(current);
            }
        }

        public void AddComponent(IGuiComponent component)
        {
            _components.Add(component);
        }

        public void RemoveComponent(IGuiComponent component)
        {
            _components.Remove(component);
        }

        /// <summary>
        ///  Calls the custom Update Method for Components. This allows components to update ui elements if they implement the the needed method.
        /// </summary>
        public void ComponentUpdate(GuiComponentType componentType, params object[] args)
        {
            var firstOrDefault = (from IGuiComponent comp in _components
                                  where comp.ComponentClass == componentType
                                  select comp).FirstOrDefault();
            if (firstOrDefault != null)
                firstOrDefault.ComponentUpdate(args);
        }

        #region Component retrieval
        /// <summary>
        ///  Returns all components of given type.
        /// </summary>
        public IEnumerable<IGuiComponent> GetComponentsByType(Type type)
        {
            return from IGuiComponent comp in _components
                   where comp.GetType() == type
                   select comp;
        }

        /// <summary>
        ///  Returns the first component with a matching Type or null if none.
        /// </summary>
        public IGuiComponent GetSingleComponentByType(Type componentType)
        {
            return (from IGuiComponent comp in _components
                    where comp.GetType() == componentType
                    select comp).FirstOrDefault();
        }

        /// <summary>
        ///  Returns the first component with a matching GuiComponentType or null if none.
        /// </summary>
        public IGuiComponent GetSingleComponentByGuiComponentType(GuiComponentType componentType)
        {
            return (from IGuiComponent comp in _components
                    where comp.ComponentClass == componentType
                    select comp).FirstOrDefault();
        }

        /// <summary>
        ///  Returns all components with matching GuiComponentType.
        /// </summary>
        public IEnumerable<IGuiComponent> GetComponentsByGuiComponentType(GuiComponentType componentType)
        {
            return from IGuiComponent comp in _components
                   where comp.ComponentClass == componentType
                   select comp;
        } 
        #endregion

        /// <summary>
        ///  Handles Net messages directed at the UI manager or components thereof. This must be called by the currently active state. See GameScreen.
        /// </summary>
        public void HandleNetMessage(NetIncomingMessage msg)
        {
            var uiMsg = (UiManagerMessage) msg.ReadByte();
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
            var component = (GuiComponentType)msg.ReadByte();
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
            foreach (var component in _components)
                component.Update();
        }

        /// <summary>
        ///  Renders UI components to screen.
        /// </summary>
        public void Render()
        {
            var renderList = from IGuiComponent comp in _components
                             where comp.IsVisible()
                             orderby comp.ZDepth ascending
                             orderby comp.Focus ascending
                             select comp;

            foreach (var component in renderList)
                component.Render();

            _cursorSprite = DragInfo.DragSprite ?? _resourceManager.GetSprite("cursor");

            _cursorSprite.Position = _mousePos;
            _cursorSprite.Draw();
        } 
        #endregion

        /// <summary>
        ///  Sets focus for a component.
        /// </summary>
        public void SetFocus(IGuiComponent newFocus)
        {
            if (_currentFocus != null)
            {
                _currentFocus.Focus = false;
                _currentFocus = newFocus;
                newFocus.Focus = true;
            }
            else
            {
                _currentFocus = newFocus;
                newFocus.Focus = true;
            }
        }

        /// <summary>
        ///  Removes focus for currently focused control.
        /// </summary>
        public void RemoveFocus()
        {
            if (_currentFocus == null) return;
            _currentFocus = null;
        }

        /// <summary>
        ///  Removes focus for given control if control has focus.
        /// </summary>
        public void RemoveFocus(IGuiComponent remFocus)
        {
            if (_currentFocus != remFocus) return;
            _currentFocus = null;
        }

        public void ResizeComponents()
        {
            foreach (var guiComponent in _components)
            {
                guiComponent.Resize();
            }
        }

        #region Input
        //The game states have to feed the UI Input!!! This is to allow more flexibility.
        //Maybe this should be handled in the main methods for input. But then the state wouldnt have power over
        //this and things like the chat might get difficult.
        //Other Notes: When a component returns true to an input event the loop stops and so only that control recieves the input.
        //             That way we don't have all components reacting to one event.

        /// <summary>
        ///  Handles MouseDown event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool MouseDown(MouseInputEventArgs e)
        {
            var inputList = from IGuiComponent comp in _components
                            where comp.RecieveInput
                            orderby comp.ZDepth ascending
                            orderby comp.IsVisible() descending //Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                            orderby comp.Focus descending
                            select comp;

            foreach (IGuiComponent current in inputList)
                if (current.MouseDown(e))
                {
                    SetFocus(current);
                    return true;
                }
            return false;
        }

        /// <summary>
        ///  Handles MouseUp event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool MouseUp(MouseInputEventArgs e)
        {
            var inputList = from IGuiComponent comp in _components
                            where comp.RecieveInput
                            orderby comp.ZDepth ascending
                            orderby comp.IsVisible() descending //Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                            orderby comp.Focus descending
                            select comp;

            if (inputList.Any(current => current.MouseUp(e))) { return true; }

            if (DragInfo.DragSprite != null || DragInfo.DragEntity != null) //Drag & dropped into nothing or invalid. Remove dragged obj.
                DragInfo.Reset();

            return false;
        }

        /// <summary>
        ///  Handles MouseMove event. Sent to all visible components.
        /// </summary>
        public virtual void MouseMove(MouseInputEventArgs e)
        {
            _mousePos = e.Position;

            var inputList = from IGuiComponent comp in _components
                            where comp.RecieveInput
                            orderby comp.ZDepth ascending
                            select comp;

            foreach (var current in inputList)
                current.MouseMove(e);
        }

        /// <summary>
        ///  Handles MouseWheelMove event. Sent to Focused component.  Returns true if component accepted and handled the event.
        /// </summary>
        public virtual void MouseWheelMove(MouseInputEventArgs e)
        {
            var inputTo = (from IGuiComponent comp in _components
                            where comp.RecieveInput
                            where comp.Focus == true
                            select comp).FirstOrDefault();

            if(inputTo != null) inputTo.MouseWheelMove(e);
        }

        /// <summary>
        ///  Handles KeyDown event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool KeyDown(KeyboardInputEventArgs e)
        {
            var inputList = from IGuiComponent comp in _components
                            where comp.RecieveInput
                            orderby comp.ZDepth ascending
                            orderby comp.IsVisible() descending //Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                            orderby comp.Focus descending
                            select comp;

            return inputList.Any(current => current.KeyDown(e));
        } 
        #endregion
    }
}
