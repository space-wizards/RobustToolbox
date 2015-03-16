using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Shared.Maths;
using Lidgren.Network;
using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.UserInterface.Components;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SS14.Client.Graphics.CluwneLib;

namespace SS14.Client.Services.UserInterface
{
    //TODO Make sure all ui compos use gorgon.currentrendertarget instead of gorgon.screen so they draw to the ui rendertarget. also add the actual rendertarget.
    /// <summary>
    ///  Manages UI Components. This includes input, rendering, updates and net messages.
    /// </summary>
    public class UserInterfaceManager : IUserInterfaceManager
    {
        /// <summary>
        ///  List of iGuiComponents. Components in this list will recieve input, updates and net messages.
        /// </summary>
        private readonly List<IGuiComponent> _components;
        private readonly IConfigurationManager _config;
        private readonly IResourceManager _resourceManager;
        private IGuiComponent _currentFocus;
		private CluwneSprite _cursorSprite;
        private DebugConsole _console;

        private Vector2 dragOffset = Vector2.Zero;
        private bool moveMode;
        private IGuiComponent movingComp;

        /// <summary>
        ///  Currently targeting action.
        /// </summary>
        private IPlayerAction targetingAction;

        public UserInterfaceManager(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            DragInfo = new DragDropInfo();
            _components = new List<IGuiComponent>();
            _config = IoCManager.Resolve<IConfigurationManager>();
            _console = new DebugConsole("dbgConsole", new Size(Gorgon.Screen.Width, 400), resourceManager);
            _console.SetVisible(false);
        }

        public Vector2 MousePos { get; private set; }

        #region IUserInterfaceManager Members

        public IDragDropInfo DragInfo { get; private set; }

        public IPlayerAction currentTargetingAction
        {
            get { return targetingAction; }
        }

        /// <summary>
        ///  Toggles UI element move mode.
        /// </summary>
        public void ToggleMoveMode()
        {
            moveMode = !moveMode;
        }

        /// <summary>
        ///  Enters targeting mode for given action.
        /// </summary>
        public void StartTargeting(IPlayerAction act)
        {
            if (act.TargetType == PlayerActionTargetType.None) return;

            IoCManager.Resolve<IPlacementManager>().Clear();
            DragInfo.Reset();

            targetingAction = act;
        }

        /// <summary>
        ///  Passes target to currently active action (and tells it to activate). Also ends targeting mode.
        /// </summary>
        public void SelectTarget(object target)
        {
            if (targetingAction == null) return;
            targetingAction.Use(target);
            CancelTargeting();
        }

        /// <summary>
        ///  Cancels targeting mode.
        /// </summary>
        public void CancelTargeting()
        {
            targetingAction = null;
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
            List<IGuiComponent> componentsOfType = (from IGuiComponent component in _components
                                                    where component.GetType() == typeof (T)
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
            var uiMsg = (UiManagerMessage) msg.ReadByte();
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

        public void ResizeComponents()
        {
            foreach (IGuiComponent guiComponent in _components)
            {
                guiComponent.Resize();
            }
        }

        #endregion

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
                    if (comp.ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
                    {
                        movingComp = comp;
                        dragOffset = (new Vector2(e.X, e.Y)) -
                                     new Vector2(comp.ClientArea.X, comp.ClientArea.Y);
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
            MousePos = new Vector2( e.X, e.Y);

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
        ///  Handles KeyDown event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool KeyDown(KeyEventArgs e)
        {
            if (e.Equals(_config.GetConsoleKey()))
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
                                                          //Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                                                          orderby comp.Focus descending
                                                          select comp;

            return inputList.Any(current => current.KeyDown(e));
        }

        #endregion

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
        ///  Handles creation of ui elements over network.
        /// </summary>
        public void HandleElementCreation(NetIncomingMessage msg) //I've opted for hardcoding these in for the moment.
        {
            var uiType = (CreateUiType) msg.ReadByte();
            switch (uiType)
            {
                case CreateUiType.HealthScannerWindow:
                    Entity ent = IoCManager.Resolve<IEntityManagerContainer>().EntityManager.GetEntity(msg.ReadInt32());
                    if (ent != null)
                    {
                        DisposeAllComponents<HealthScannerWindow>();
                        var scannerWindow = new HealthScannerWindow(ent, MousePos, this, _resourceManager);
                        AddComponent(scannerWindow);
                    }
                    break;
            }
        }

        /// <summary>
        ///  Handles and reroutes Net messages directed at components.
        /// </summary>
        public void HandleComponentMessage(NetIncomingMessage msg)
        {
            var component = (GuiComponentType) msg.ReadByte();
            IEnumerable<IGuiComponent> targetComponents = GetComponentsByGuiComponentType(component);
            foreach (IGuiComponent current in targetComponents)
                current.HandleNetworkMessage(msg);
        }

        /// <summary>
        ///  Removes focus for given control if control has focus.
        /// </summary>
        public void RemoveFocus(IGuiComponent remFocus)
        {
            if (_currentFocus != remFocus) return;
            _currentFocus = null;
        }

        #region Update & Render

        //These methods are called directly from the main loop to allow for cross-state ui elements. (Console maybe)

        /// <summary>
        ///  Updates the logic of UI components.
        /// </summary>
        public void Update(float frameTime)
        {
            if(_console.IsVisible()) _console.Update(frameTime);

            if (moveMode && movingComp != null)
                movingComp.Position = (Point) (MousePos - dragOffset);

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
                */}
            }

            if (targetingAction != null)
            {
                _cursorSprite = _resourceManager.GetSprite("cursor_target");
            }
            else
            {
                _cursorSprite = DragInfo.DragSprite != null && DragInfo.IsActive
                                    ? DragInfo.DragSprite
                                    : _resourceManager.GetSprite("cursor");
            }

            if (_console.IsVisible()) _console.Render();

            _cursorSprite.Position = MousePos;
            _cursorSprite.Draw();
        }

        #endregion
    }
} 