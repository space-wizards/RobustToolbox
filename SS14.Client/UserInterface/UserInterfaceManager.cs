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

        public UITheme Theme { get; private set; }
        public Control Focused { get; private set; }

        private Godot.CanvasLayer CanvasLayer;
        public Control StateRoot { get; private set; }
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
            Theme = new UIThemeDefault();

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

            _render(drawHandle, RootControl, Vector2.Zero);
        }

        private static void _render(DrawingHandleScreen handle, Control control, Vector2 position)
        {
            if (!control.Visible)
            {
                return;
            }

            handle.SetTransform(position, Angle.Zero, Vector2.One);
            control.Draw(handle);
            foreach (var child in control.Children)
            {
                _render(handle, child, position + child.Position.Rounded());
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
    }
}
