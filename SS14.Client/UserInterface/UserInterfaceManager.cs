using System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Clyde;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Input;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.Configuration;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface
{
    internal sealed class UserInterfaceManager : IPostInjectInit, IDisposable, IUserInterfaceManagerInternal
    {
        [Dependency] private readonly IConfigurationManager _config;
        [Dependency] private readonly ISceneTreeHolder _sceneTreeHolder;
        [Dependency] private readonly IInputManager _inputManager;

        public Control Focused { get; private set; }

        private Godot.CanvasLayer CanvasLayer;
        public Control StateRoot { get; private set; }
        public Control RootControl { get; private set; }
        public Control WindowRoot { get; private set; }
        public AcceptDialog PopupControl { get; private set; }
        public DebugConsole DebugConsole { get; private set; }
        public IDebugMonitors DebugMonitors => _debugMonitors;
        private DebugMonitors _debugMonitors;

        public void PostInject()
        {
            _config.RegisterCVar("key.keyboard.console", Keyboard.Key.Tilde, CVar.ARCHIVE);
        }

        public void Initialize()
        {
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

        public void PreKeyDown(KeyEventArgs args)
        {
            if (args.Key == Keyboard.Key.Quote)
            {
                DebugConsole.Toggle();
                args.Handle();
            }
        }

        public void PreKeyUp(KeyEventArgs args)
        {
        }

        public void Render(IRenderHandle renderHandle)
        {
            var drawHandle = renderHandle.CreateHandleScreen();

            _render(drawHandle, RootControl);
        }

        private void _render(DrawingHandleScreen handle, Control control)
        {
            control.Draw(handle);
            foreach (var child in control.Children)
            {
                _render(handle, child);
            }
        }

        public void UnhandledMouseDown(MouseButtonEventArgs args)
        {
            Focused?.ReleaseFocus();
        }

        public void UnhandledMouseUp(MouseButtonEventArgs args)
        {
            //throw new System.NotImplementedException();
        }

        public void FocusEntered(Control control)
        {
            Focused = control;
        }

        public void FocusExited(Control control)
        {
            if (Focused == control)
            {
                Focused = null;
            }
        }
    }
}
