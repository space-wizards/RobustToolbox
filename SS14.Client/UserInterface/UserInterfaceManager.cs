using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Clyde;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Input;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Client.Utility;
using SS14.Shared.Configuration;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface
{
    internal sealed class UserInterfaceManager : IPostInjectInit, IDisposable, IUserInterfaceManagerInternal
    {
        [Dependency] private readonly IConfigurationManager _config;
        [Dependency] private readonly ISceneTreeHolder _sceneTreeHolder;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IDisplayManager _displayManager;
        [Dependency] private readonly IResourceCache _resourceCache;

        public UITheme ThemeDefaults { get; private set; }
        public Stylesheet Stylesheet { get; set; }
        public Control Focused { get; private set; }

        // When a control receives a mouse down it must also receive a mouse up and mouse moves, always.
        // So we keep track of which control is "focused" by the mouse.
        private Control _mouseFocused;

        private Godot.CanvasLayer CanvasLayer;
        public Control StateRoot { get; private set; }
        public Control CurrentlyHovered { get; private set; }
        public Control RootControl { get; private set; }
        public Control WindowRoot { get; private set; }
        public AcceptDialog PopupControl { get; private set; }
        public DebugConsole DebugConsole { get; private set; }
        public IDebugMonitors DebugMonitors => _debugMonitors;
        private DebugMonitors _debugMonitors;

        public Dictionary<(GodotAsset asset, int resourceId), object> GodotResourceInstanceCache { get; } =
            new Dictionary<(GodotAsset asset, int resourceId), object>();

        public void PostInject()
        {
            _config.RegisterCVar("key.keyboard.console", Keyboard.Key.Tilde, CVar.ARCHIVE);
        }

        public void Initialize()
        {
            ThemeDefaults = new UIThemeDefault();

            if (GameController.OnGodot)
            {
                CanvasLayer = new Godot.CanvasLayer
                {
                    Name = "UILayer",
                    Layer = CanvasLayers.LAYER_GUI
                };

                _sceneTreeHolder.SceneTree.GetRoot().AddChild(CanvasLayer);
            }

            RootControl = new Control("UIRoot")
            {
                MouseFilter = Control.MouseFilterMode.Ignore
            };
            RootControl.SetAnchorPreset(Control.LayoutPreset.Wide);
            if (!GameController.OnGodot)
            {
                RootControl.Size = _displayManager.ScreenSize;
                _displayManager.OnWindowResized += args => RootControl.Size = args.NewSize;
            }

            if (GameController.OnGodot)
            {
                CanvasLayer.AddChild(RootControl.SceneControl);
            }

            StateRoot = new Control("StateRoot")
            {
                MouseFilter = Control.MouseFilterMode.Ignore
            };
            StateRoot.SetAnchorPreset(Control.LayoutPreset.Wide);
            RootControl.AddChild(StateRoot);

            WindowRoot = new Control("WindowRoot");
            WindowRoot.MouseFilter = Control.MouseFilterMode.Ignore;
            WindowRoot.SetAnchorPreset(Control.LayoutPreset.Wide);
            RootControl.AddChild(WindowRoot);

            PopupControl = new AcceptDialog("RootPopup");
            PopupControl.Resizable = true;
            RootControl.AddChild(PopupControl);

            DebugConsole = new DebugConsole();
            RootControl.AddChild(DebugConsole);

            _debugMonitors = new DebugMonitors();
            RootControl.AddChild(_debugMonitors);

            _inputManager.SetInputCommand(EngineKeyFunctions.ShowDebugMonitors,
                InputCmdHandler.FromDelegate(enabled: session => { DebugMonitors.Visible = true; },
                    disabled: session => { DebugMonitors.Visible = false; }));
        }

        public void Dispose()
        {
            RootControl.Dispose();
        }

        public void Update(ProcessFrameEventArgs args)
        {
            RootControl.DoUpdate(args);
        }

        public void FrameUpdate(RenderFrameEventArgs args)
        {
            RootControl.DoFrameUpdate(args);
        }

        public void MouseDown(MouseButtonEventArgs args)
        {
            var control = MouseGetControl(args.Position);
            if (control == null)
            {
                return;
            }

            _mouseFocused = control;
            var guiArgs = new GUIMouseButtonEventArgs(args.Button, args.DoubleClick, control, Mouse.ButtonMask.None,
                args.Position, args.Position - control.GlobalPosition, args.Alt, args.Control, args.Shift,
                args.System);

            _doMouseGuiInput(control, guiArgs, (c, ev) => c.MouseDown(ev));
        }

        public void MouseUp(MouseButtonEventArgs args)
        {
            if (_mouseFocused == null)
            {
                return;
            }

            var guiArgs = new GUIMouseButtonEventArgs(args.Button, args.DoubleClick, _mouseFocused,
                Mouse.ButtonMask.None,
                args.Position, args.Position - _mouseFocused.GlobalPosition, args.Alt, args.Control, args.Shift,
                args.System);

            _doMouseGuiInput(_mouseFocused, guiArgs, (c, ev) => c.MouseUp(ev));
            _mouseFocused.MouseUp(guiArgs);
            _mouseFocused = null;
        }

        public void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
        {
            // Update which control is considered hovered.
            var newHovered = _mouseFocused ?? MouseGetControl(mouseMoveEventArgs.Position);
            if (newHovered != CurrentlyHovered)
            {
                CurrentlyHovered?.MouseExited();
                CurrentlyHovered = newHovered;
                CurrentlyHovered?.MouseEntered();
            }

            if (newHovered != null)
            {
                var guiArgs = new GUIMouseMoveEventArgs(mouseMoveEventArgs.Relative, mouseMoveEventArgs.Speed, newHovered,
                    mouseMoveEventArgs.ButtonMask, mouseMoveEventArgs.Position,
                    mouseMoveEventArgs.Position - newHovered.GlobalPosition, mouseMoveEventArgs.Alt,
                    mouseMoveEventArgs.Control, mouseMoveEventArgs.Shift, mouseMoveEventArgs.System);

                _doMouseGuiInput(_mouseFocused, guiArgs, (c, ev) => c.MouseMove(ev));
            }
        }

        public void DisposeAllComponents()
        {
            RootControl.DisposeAllChildren();
        }

        public void Popup(string contents, string title = "Alert!")
        {
            PopupControl.DialogText = contents;
            PopupControl.Title = title;
            PopupControl.OpenMinimum();
        }

        public Control MouseGetControl(Vector2 coordinates)
        {
            return _mouseFindControlAtPos(RootControl, coordinates);
        }

        public void GDPreKeyDown(KeyEventArgs args)
        {
            if (args.Key == Keyboard.Key.Quote)
            {
                DebugConsole.Toggle();
                args.Handle();
            }
        }

        public void GDPreKeyUp(KeyEventArgs args)
        {
        }

        public void Render(IRenderHandle renderHandle)
        {
            var drawHandle = renderHandle.CreateHandleScreen();

            _render(drawHandle, RootControl, Vector2.Zero, Color.White);
        }

        private static void _render(DrawingHandleScreen handle, Control control, Vector2 position, Color modulate)
        {
            if (!control.Visible)
            {
                return;
            }

            handle.SetTransform(position, Angle.Zero, Vector2.One);
            handle.Modulate = modulate * control.ActualModulateSelf;
            control.Draw(handle);
            foreach (var child in control.Children)
            {
                _render(handle, child, position + child.Position.Rounded(), modulate);
            }
        }

        public void GDUnhandledMouseDown(MouseButtonEventArgs args)
        {
            Focused?.ReleaseFocus();
        }

        public void GDUnhandledMouseUp(MouseButtonEventArgs args)
        {
            //throw new System.NotImplementedException();
        }

        public void GDFocusEntered(Control control)
        {
            Focused = control;
        }

        public void GDFocusExited(Control control)
        {
            if (Focused == control)
            {
                Focused = null;
            }
        }

        public void GDMouseEntered(Control control)
        {
            CurrentlyHovered = control;
        }

        public void GDMouseExited(Control control)
        {
            if (control == CurrentlyHovered)
            {
                CurrentlyHovered = null;
            }
        }

        private Control _mouseFindControlAtPos(Control control, Vector2 position)
        {
            foreach (var child in control.Children.Reverse())
            {
                if (!child.Visible)
                {
                    continue;
                }

                var maybeFoundOnChild = _mouseFindControlAtPos(child, position - child.Position);
                if (maybeFoundOnChild != null)
                {
                    return maybeFoundOnChild;
                }
            }

            if (control.MouseFilter != Control.MouseFilterMode.Ignore && control.HasPoint(position))
            {
                return control;
            }

            return null;
        }

        private void _doMouseGuiInput<T>(Control control, T guiEvent, Action<Control, T> action) where T : GUIMouseEventArgs
        {
            while (control != null)
            {
                if (control.MouseFilter != Control.MouseFilterMode.Ignore)
                {
                    action(control, guiEvent);

                    if (guiEvent.Handled || control.MouseFilter == Control.MouseFilterMode.Stop)
                    {
                        break;
                    }
                }

                guiEvent.RelativePosition += control.Position;
                control = control.Parent;
                guiEvent.SourceControl = control;
            }
        }
    }
}
