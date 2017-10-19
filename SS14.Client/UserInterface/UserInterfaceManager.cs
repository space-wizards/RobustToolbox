using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using SFML.Graphics;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Console;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface.Components;
using SS14.Shared;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.UserInterface
{
    //TODO Make sure all ui compos use gorgon.currentrendertarget instead of gorgon.screen so they draw to the ui rendertarget. also add the actual rendertarget.
    /// <summary>
    ///     Manages UI Components. This includes input, rendering, updates and net messages.
    /// </summary>
    public class UserInterfaceManager : IUserInterfaceManager, IPostInjectInit
    {
        /// <summary>
        ///     List of iGuiComponents. Components in this list will recieve input, updates and net messages.
        /// </summary>
        private readonly List<Control> _components = new List<Control>();

        [Dependency]
        private readonly IConfigurationManager _config;

        [Dependency]
        private readonly IResourceCache _resourceCache;

        private Control _currentFocus;
        private Sprite _cursorSprite;
        private DebugConsole _console;

        private Vector2i dragOffset;
        private bool moveMode;
        private Control movingComp;
        private bool showCursor = true;

        public Vector2i MousePos { get; private set; }

        public void PostInject()
        {
            _config.RegisterCVar("key.keyboard.console", Keyboard.Key.Home, CVar.ARCHIVE);
        }

        public void Initialize()
        {
            _console = new DebugConsole("dbgConsole", new Vector2i((int) CluwneLib.Window.Viewport.Size.X, 400), _resourceCache);
            _console.SetVisible(false);
        }

        #region Component retrieval

        /// <summary>
        ///     Returns all components with matching GuiComponentType.
        /// </summary>
        public IEnumerable<Control> GetComponentsByGuiComponentType(GuiComponentType componentType)
        {
            return from Control comp in _components
                where comp.ComponentClass == componentType
                select comp;
        }

        #endregion Component retrieval

        #region IUserInterfaceManager Members

        public IDragDropInfo DragInfo { get; } = new DragDropInfo();

        public IDebugConsole Console => _console;

        /// <summary>
        ///     Toggles UI element move mode.
        /// </summary>
        public void ToggleMoveMode()
        {
            moveMode = !moveMode;
        }

        /// <summary>
        ///     Disposes all components and clears component list. Components need to implement their own Dispose method.
        /// </summary>
        public void DisposeAllComponents()
        {
            foreach (var x in _components.ToList())
            {
                x.Dispose();
            }
            _components.Clear();
        }

        /// <summary>
        ///     Disposes all components of the given type.
        /// </summary>
        public void DisposeAllComponents<T>()
        {
            var componentsOfType = (from Control component in _components
                where component.GetType() == typeof(T)
                select component).ToList();

            foreach (var current in componentsOfType)
            {
                current.Dispose();
                _components.Remove(current);
            }
        }

        public void AddComponent(Control component)
        {
            _components.Add(component);
        }

        public void RemoveComponent(Control component)
        {
            if (_components.Contains(component))
                _components.Remove(component);
        }

        //TODO Holy shit make this not complete crap. Oh man.

        /// <summary>
        ///     Sets focus for a component.
        /// </summary>
        public void SetFocus(Control newFocus)
        {
            if (_currentFocus != null)
                RemoveFocus();
            _currentFocus = newFocus;
            newFocus.Focus = true;
        }

        /// <summary>
        ///     Removes focus for currently focused control.
        /// </summary>
        public void RemoveFocus()
        {
            if (_currentFocus == null)
                return;

            _currentFocus.Focus = false;
            _currentFocus = null;
        }

        /// <summary>
        ///     Removes focus for given control if control has focus.
        /// </summary>
        public void RemoveFocus(Control remFocus)
        {
            if (_currentFocus != remFocus)
                return;

            _currentFocus.Focus = false;
            _currentFocus = null;
        }

        public void ResizeComponents()
        {
            foreach (var guiComponent in _components)
            {
                guiComponent.DoLayout();
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
        ///     Handles MouseDown event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool MouseDown(MouseButtonEventArgs e)
        {
            if (_console.IsVisible())
                if (_console.MouseDown(e)) return true;

            if (moveMode)
            {
                foreach (var comp in _components)
                {
                    if (comp.ClientArea.Contains(e.X, e.Y))
                    {
                        movingComp = comp;
                        dragOffset = new Vector2i(e.X, e.Y) -
                                     new Vector2i(comp.ClientArea.Left, comp.ClientArea.Top);
                        break;
                    }
                }
                return true;
            }
            var inputList = from Control comp in _components
                where comp.ReceiveInput
                orderby comp.ZDepth
                orderby comp.IsVisible() descending
                //Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                orderby comp.Focus descending
                select comp;

            foreach (var current in inputList)
            {
                if (current.MouseDown(e))
                {
                    SetFocus(current);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Handles MouseUp event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool MouseUp(MouseButtonEventArgs e)
        {
            if (_console.IsVisible())
                if (_console.MouseUp(e)) return true;

            if (moveMode)
            {
                if (movingComp != null) movingComp = null;
                return true;
            }
            var inputList = from Control comp in _components
                where comp.ReceiveInput
                orderby comp.ZDepth
                orderby comp.IsVisible() descending
                //Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                orderby comp.Focus descending
                select comp;

            if (inputList.Any(current => current.MouseUp(e)))
                return true;

            if (DragInfo.IsActive) //Drag & dropped into nothing or invalid. Remove dragged obj.
                DragInfo.Reset();

            return false;
        }

        /// <summary>
        ///     Handles MouseMove event. Sent to all visible components.
        /// </summary>
        public virtual void MouseMove(MouseMoveEventArgs e)
        {
            MousePos = new Vector2i(e.X, e.Y);

            if (_console.IsVisible())
                _console.MouseMove(e);

            var inputList = from Control comp in _components
                where comp.ReceiveInput
                orderby comp.ZDepth
                select comp;

            foreach (var current in inputList)
            {
                current.MouseMove(e);
            }
        }

        /// <summary>
        ///     Handles MouseWheelMove event. Sent to Focused component.  Returns true if component accepted and handled the event.
        /// </summary>
        public virtual void MouseWheelMove(MouseWheelScrollEventArgs e)
        {
            if (_console.IsVisible())
                _console.MouseWheelMove(e);

            var inputTo = (from Control comp in _components
                where comp.ReceiveInput
                where comp.Focus
                select comp).FirstOrDefault();

            if (inputTo != null) inputTo.MouseWheelMove(e);
        }

        /// <summary>
        ///     Handles MouseEntered event. Not sent to Focused component.
        /// </summary>
        public virtual void MouseEntered(EventArgs e)
        {
            showCursor = true;
        }

        /// <summary>
        ///     Handles MouseLeft event. Not sent to Focused component.
        /// </summary>
        public virtual void MouseLeft(EventArgs e)
        {
            showCursor = false;
        }

        /// <summary>
        ///     Handles KeyDown event. Returns true if a component accepted and handled the event.
        /// </summary>
        public virtual bool KeyDown(KeyEventArgs e)
        {
            if (e.Code == _config.GetCVar<Keyboard.Key>("key.keyboard.console"))
            {
                _console.ToggleVisible();
                return true;
            }

            if (_console.IsVisible())
                if (_console.KeyDown(e)) return true;

            var inputList = from Control comp in _components
                where comp.ReceiveInput
                orderby comp.ZDepth
                orderby comp.IsVisible() descending
                // Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                orderby comp.Focus descending
                select comp;

            return inputList.Any(current => current.KeyDown(e));
        }

        public virtual bool TextEntered(TextEventArgs e)
        {
            if (_console.IsVisible())
                if (_console.TextEntered(e)) return true;

            var inputList = from Control comp in _components
                where comp.ReceiveInput
                orderby comp.ZDepth
                orderby comp.IsVisible() descending
                // Invisible controls still recieve input but after everyone else. This is mostly for the inventory and other toggleable components.
                orderby comp.Focus descending
                select comp;

            return inputList.Any(current => current.TextEntered(e));
        }

        #endregion Input

        #region Update & Render

        //These methods are called directly from the main loop to allow for cross-state ui elements. (Console maybe)

        /// <summary>
        ///     Updates the logic of UI components.
        /// </summary>
        public void Update(FrameEventArgs e)
        {
            if (_console.IsVisible()) _console.Update((float) e.Time);

            if (moveMode && movingComp != null)
                movingComp.Position = MousePos - dragOffset;

            foreach (var component in _components)
            {
                component.Update((float) e.Time);
            }
        }

        /// <summary>
        ///     Renders UI components to screen.
        /// </summary>
        public void Render(FrameEventArgs e)
        {
            var renderList = from Control comp in _components
                where comp.IsVisible()
                orderby comp.Focus
                orderby comp.ZDepth
                select comp;

            foreach (var component in renderList)
            {
                component.Draw();
            }

            if (_console.IsVisible()) _console.Draw();

            if (showCursor)
            {
                _cursorSprite = DragInfo.DragSprite != null && DragInfo.IsActive
                    ? DragInfo.DragSprite
                    : _resourceCache.GetSprite("cursor");

                _cursorSprite.Position = ((Vector2) MousePos).Convert();
                _cursorSprite.Draw();
            }
        }

        #endregion Update & Render
    }
}
