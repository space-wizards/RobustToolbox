using SS14.Client.Input;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.ResourceManagement;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using System.Collections.Generic;

namespace SS14.Client.UserInterface
{
    public sealed class UserInterfaceManager : IUserInterfaceManager, IPostInjectInit
    {
        [Dependency]
        readonly IConfigurationManager _config;
        [Dependency]
        readonly ISceneTreeHolder _sceneTreeHolder;

        public Control Focused { get; private set; }

        private Godot.CanvasLayer CanvasLayer;
        public Control StateRoot { get; private set; }
        public Control RootControl { get; private set; }
        public AcceptDialog PopupControl { get; private set; }
        public DebugConsole DebugConsole { get; private set; }
        public FPSCounter FPSCounter { get; private set; }

        public bool ShowFPS
        {
            get => FPSCounter.Visible;
            set => FPSCounter.Visible = value;
        }

        public void PostInject()
        {
            _config.RegisterCVar("key.keyboard.console", Keyboard.Key.Tilde, CVar.ARCHIVE);
        }

        public void Initialize()
        {
            CanvasLayer = new Godot.CanvasLayer();
            CanvasLayer.SetName("UILayer");

            _sceneTreeHolder.SceneTree.GetRoot().AddChild(CanvasLayer);

            RootControl = new Control("UIRoot");
            RootControl.SetAnchorPreset(Control.AnchorPreset.Wide);

            CanvasLayer.AddChild(RootControl.SceneControl);

            StateRoot = new Control("StateRoot");
            StateRoot.SetAnchorPreset(Control.AnchorPreset.Wide);
            RootControl.AddChild(StateRoot);

            PopupControl = new AcceptDialog("RootPopup");
            RootControl.AddChild(PopupControl);

            DebugConsole = new DebugConsole();
            RootControl.AddChild(DebugConsole);

            FPSCounter = new FPSCounter();
            RootControl.AddChild(FPSCounter);
        }

        public void Update(FrameEventArgs args)
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
                _sceneTreeHolder.SceneTree.SetInputAsHandled();
            }
        }

        public void PreKeyUp(KeyEventArgs args)
        {
        }

        public void UnhandledKeyDown(KeyEventArgs args)
        {
            // Nothing
        }

        public void UnhandledKeyUp(KeyEventArgs args)
        {
            // Nothing
        }

        public void UnhandledMouseDown(MouseButtonEventArgs args)
        {
            //throw new System.NotImplementedException();
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
