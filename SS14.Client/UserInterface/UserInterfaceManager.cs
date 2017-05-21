using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.Console;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface.Components;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.UserInterface
{
    //TODO Make sure all ui compos use gorgon.currentrendertarget instead of gorgon.screen so they draw to the ui rendertarget. also add the actual rendertarget.
    /// <summary>
    ///  Manages UI Components. This includes input, rendering, updates and net messages.
    /// </summary>
    [IoCTarget]
    public class UserInterfaceManager : IUserInterfaceManager
    {
        /// <summary>
        ///  List of iGuiComponents. Components in this list will recieve input, updates and net messages.
        /// </summary>
        private readonly List<IGuiComponent> _components;

        private readonly IPlayerConfigurationManager _config;
        private readonly IResourceManager _resourceManager;
        private IGuiComponent _currentFocus;
        private Sprite _cursorSprite;
        private DebugConsole _console;

        private Vector2i dragOffset = new Vector2i();
        private bool moveMode;
        private IGuiComponent movingComp;
        private bool showCursor = true;

        /// <summary>
        ///  Currently targeting action.
        /// </summary>

        public UserInterfaceManager(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            DragInfo = new DragDropInfo();
            _components = new List<IGuiComponent>();
            _config = IoCManager.Resolve<IPlayerConfigurationManager>();
            _console = new DebugConsole("dbgConsole", new Vector2i((int)CluwneLib.Screen.Size.X, 400), resourceManager);
            _console.SetVisible(false);
        }

        public Vector2i MousePos { get; private set; }

        #region IUserInterfaceManager Members

        public IDragDropInfo DragInfo { get; private set; }

        public IDebugConsole Console => _console;

        /// <summary>
        ///  Toggles UI element move mode.
        /// </summary>
        public void ToggleMoveMode()
        {
            moveMode = !moveMode;
        }

        /// <summary>
        ///  Disposes all components and clears component list. Components need to implement their own Dispose method.
        /// </summary>
        public void DisposeAllComponents()
        {
            foreach (IGuiComponent x in _components.ToList())
            {
                x.Dispose();
            }
            _components.Clear();
        }

        /// <summary>
        ///  Disposes all components of the given type.
        /// </summary>
        public void DisposeAllComponents<T>()
        {
            List<IGuiComponent> componentsOfType = (from IGuiComponent component in _components
                                                    where component.GetType() == typeof(T)
                                                    select component).ToList();

            foreach (IGuiComponent current in componentsOfType)
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
            if (_components.Contains(component))
                _components.Remove(component);
        }

        /// <summary>
        ///  Calls the custom Update Method for Components. This allows components to update ui elements if they implement the the needed method.
        /// </summary>
        public void ComponentUpdate(GuiComponentType componentType, params object[] args)
        {
            IGuiComponent firstOrDefault = (from IGuiComponent comp in _components
                                            where comp.ComponentClass == componentType
                                            select comp).FirstOrDefault();
            if (firstOrDefault != null)
                firstOrDefault.ComponentUpdate(args);
        }

        /// <summary>
        ///  Handles Net messages directed at the UI manager or components thereof. This must be called by the currently active state. See GameScreen.
        /// </summary>
        public void HandleNetMessage(NetIncomingMessage msg)
        {
            var uiMsg = (UiManagerMessage)msg.ReadByte();
            switch (uiMsg)
            {
                case UiManagerMessage.ComponentMessage:
                    HandleComponentMessage(msg);
                    break;

                case UiManagerMessage.CreateUiElement:
                    HandleElementCreation(msg);
                    break;
            }
        }

        //TODO Holy shit make this not complete crap. Oh man.

        /// <summary>
        ///  Sets focus for a component.
        /// </summary>
        public void SetFocus(IGuiComponent newFocus)
        {
            if (_currentFocus != null)
            {
                RemoveFocus();
            }
            _currentFocus = newFocus;
            newFocus.Focus = true;
        }

        /// <summary>
        ///  Removes focus for currently focused control.
        /// </summary>
        public void RemoveFocus()
        {
            if (_currentFocus == null)
                return;

            _currentFocus.Focus = false;
            _currentFocus = null;
        }

        /// <summary>
        ///  Removes focus for given control if control has focus.
        /// </summary>
        public void RemoveFocus(IGuiComponent remFocus)
        {
            if (_currentFocus != remFocus)
                return;

            _currentFocus.Focus = false;
            _currentFocus = null;
        }

        public void ResizeComponents()
        {
            foreach (IGuiComponent guiComponent in _components)
            {
                guiComponent.Resize();
            }
        }

        #endregion IUserInterfaceManager Members

        #region Input

        //The game states have to feed the UI Input!!! This is to allow more flexibility.
        //Maybe this should be handled in the main methods for input. But then the state wouldnt have power over
        //this and things like the chat might get difficult.
        //Other Notes: When a component returns true to an input event the loop stops and so only that control recieves the input.
        //             That way we don't have all components reacting to one event.

        /// <summary>
        ///  Handles MouseDown event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool MouseDown(MouseButtonEventArgs e)
        {
            if (_console.IsVisible())
            {
                if (_console.MouseDown(e)) return true;
            }

            if (moveMode)
            {
                foreach (IGuiComponent comp in _components)
                {
                    if (comp.ClientArea.Contains(e.X, e.Y))
                    {
                        movingComp = comp;
                        dragOffset = (new Vector2i(e.X, e.Y)) -
                                     new Vector2i(comp.ClientArea.Left, comp.ClientArea.Top);
                        break;
                    }
                }
                return true;
            }
            else
            {
                IOrderedEnumerable<IGuiComponent> inputList = from IGuiComponent comp in _components
                                                              where comp.RecieveInput
                                                              orderby comp.ZDepth ascending
                                                              orderby comp.IsVisible() descending
                                                              //Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
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
        }

        /// <summary>
        ///  Handles MouseUp event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool MouseUp(MouseButtonEventArgs e)
        {
            if (_console.IsVisible())
            {
                if (_console.MouseUp(e)) return true;
            }

            if (moveMode)
            {
                if (movingComp != null) movingComp = null;
                return true;
            }
            else
            {
                IOrderedEnumerable<IGuiComponent> inputList = from IGuiComponent comp in _components
                                                              where comp.RecieveInput
                                                              orderby comp.ZDepth ascending
                                                              orderby comp.IsVisible() descending
                                                              //Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                                                              orderby comp.Focus descending
                                                              select comp;

                if (inputList.Any(current => current.MouseUp(e)))
                {
                    return true;
                }

                if (DragInfo.IsActive) //Drag & dropped into nothing or invalid. Remove dragged obj.
                    DragInfo.Reset();

                return false;
            }
        }

        /// <summary>
        ///  Handles MouseMove event. Sent to all visible components.
        /// </summary>
        public virtual void MouseMove(MouseMoveEventArgs e)
        {
            MousePos = new Vector2i(e.X, e.Y);

            if (_console.IsVisible())
            {
                _console.MouseMove(e);
            }

            IOrderedEnumerable<IGuiComponent> inputList = from IGuiComponent comp in _components
                                                          where comp.RecieveInput
                                                          orderby comp.ZDepth ascending
                                                          select comp;

            foreach (IGuiComponent current in inputList)
                current.MouseMove(e);
        }

        /// <summary>
        ///  Handles MouseWheelMove event. Sent to Focused component.  Returns true if component accepted and handled the event.
        /// </summary>
        public virtual void MouseWheelMove(MouseWheelEventArgs e)
        {
            if (_console.IsVisible())
            {
                _console.MouseWheelMove(e);
            }

            IGuiComponent inputTo = (from IGuiComponent comp in _components
                                     where comp.RecieveInput
                                     where comp.Focus
                                     select comp).FirstOrDefault();

            if (inputTo != null) inputTo.MouseWheelMove(e);
        }

        /// <summary>
        ///  Handles MouseEntered event. Not sent to Focused component.
        /// </summary>
        public virtual void MouseEntered(EventArgs e)
        {
            showCursor = true;
        }

        /// <summary>
        ///  Handles MouseLeft event. Not sent to Focused component.
        /// </summary>
        public virtual void MouseLeft(EventArgs e)
        {
            showCursor = false;
        }

        /// <summary>
        ///  Handles KeyDown event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool KeyDown(KeyEventArgs e)
        {
            if (e.Code == _config.GetConsoleKey())
            {
                _console.ToggleVisible();
                return true;
            }

            if (_console.IsVisible())
            {
                if (_console.KeyDown(e)) return true;
            }

            IOrderedEnumerable<IGuiComponent> inputList = from IGuiComponent comp in _components
                                                          where comp.RecieveInput
                                                          orderby comp.ZDepth ascending
                                                          orderby comp.IsVisible() descending
                                                          // Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                                                          orderby comp.Focus descending
                                                          select comp;

            return inputList.Any(current => current.KeyDown(e));
        }

        public virtual bool TextEntered(TextEventArgs e)
        {
            if (_console.IsVisible())
            {
                if (_console.TextEntered(e)) return true;
            }

            IOrderedEnumerable<IGuiComponent> inputList = from IGuiComponent comp in _components
                                                          where comp.RecieveInput
                                                          orderby comp.ZDepth ascending
                                                          orderby comp.IsVisible() descending
                                                          // Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                                                          orderby comp.Focus descending
                                                          select comp;

            return inputList.Any(current => current.TextEntered(e));
        }

        #endregion Input

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

        #endregion Component retrieval

        /// <summary>
        ///  Handles creation of ui elements over network.
        /// </summary>
        public void HandleElementCreation(NetIncomingMessage msg) //I've opted for hardcoding these in for the moment.
        {
            /*
            var uiType = (CreateUiType)msg.ReadByte();
            switch (uiType)
            {

            }
            */
        }

        /// <summary>
        ///  Handles and reroutes Net messages directed at components.
        /// </summary>
        public void HandleComponentMessage(NetIncomingMessage msg)
        {
            var component = (GuiComponentType)msg.ReadByte();
            IEnumerable<IGuiComponent> targetComponents = GetComponentsByGuiComponentType(component);
            foreach (IGuiComponent current in targetComponents)
                current.HandleNetworkMessage(msg);
        }

        #region Update & Render

        //These methods are called directly from the main loop to allow for cross-state ui elements. (Console maybe)

        /// <summary>
        ///  Updates the logic of UI components.
        /// </summary>
        public void Update(float frameTime)
        {
            if (_console.IsVisible()) _console.Update(frameTime);

            if (moveMode && movingComp != null)
                movingComp.Position = (MousePos - dragOffset);

            foreach (IGuiComponent component in _components)
                component.Update(frameTime);
        }

        /// <summary>
        ///  Renders UI components to screen.
        /// </summary>
        public void Render()
        {
            IOrderedEnumerable<IGuiComponent> renderList = from IGuiComponent comp in _components
                                                           where comp.IsVisible()
                                                           orderby comp.Focus ascending
                                                           orderby comp.ZDepth ascending
                                                           select comp;

            foreach (IGuiComponent component in renderList)
            {
                component.Render();

                if (moveMode)
                { /*
                    CluwneLib.Screen.BlendingMode = BlendingModes.Modulated;
                   CluwneLib.Screen.FilledRectangle(component.ClientArea.X, component.ClientArea.Y,
                                                  component.ClientArea.Width, component.ClientArea.Height,
                                                  Color.FromArgb(100, Color.Green));
                    CluwneLib.Screen.Rectangle(component.ClientArea.X, component.ClientArea.Y, component.ClientArea.Width,
                                            component.ClientArea.Height, Color.LightGreen);
                   CluwneLib.Screen.BlendingMode = BlendingModes.None;
                */
                }
            }

            if (_console.IsVisible()) _console.Render();

            if (showCursor)
            {
                _cursorSprite = DragInfo.DragSprite != null && DragInfo.IsActive
                                    ? DragInfo.DragSprite
                                    : _resourceManager.GetSprite("cursor");

                _cursorSprite.Position = MousePos.ToFloat();
                _cursorSprite.Draw();
            }
        }

        #endregion Update & Render
    }
}
