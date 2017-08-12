using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.State.States;
using SS14.Shared.Maths;
using Vector2i = SFML.System.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    public class DisconnectedScreenBlocker : GuiComponent
    {
        private readonly Button _mainMenuButton;
        private readonly Label _message;
        private readonly IResourceCache _resourceCache;
        private readonly IStateManager _stateManager;
        private readonly IUserInterfaceManager _userInterfaceManager;

        public DisconnectedScreenBlocker(IStateManager stateManager, IUserInterfaceManager userInterfaceManager,
                                         IResourceCache resourceCache, string message = "Connection closed.")
        {
            _stateManager = stateManager;
            _resourceCache = resourceCache;
            _userInterfaceManager = userInterfaceManager;
            _userInterfaceManager.DisposeAllComponents();

            _message = new Label(message, "CALIBRI", _resourceCache);
            _mainMenuButton = new Button("Main Menu", _resourceCache);
            _mainMenuButton.Clicked += MainMenuButtonClicked;
            _mainMenuButton.Label.Color = new SFML.Graphics.Color(245, 245, 245);
            _message.Text.Color = new SFML.Graphics.Color(245, 245, 245);
        }

        private void MainMenuButtonClicked(Button sender)
        {
            _stateManager.RequestStateChange<MainScreen>();
        }

        public override void Update(float frameTime)
        {
            _message.Position = new Vector2i((int) (CluwneLib.CurrentRenderTarget.Size.X/2f - _message.ClientArea.Width/2f),
                                          (int) (CluwneLib.CurrentRenderTarget.Size.Y/2f - _message.ClientArea.Height/2f) -
                                          50);
            _message.Update(frameTime);
            _mainMenuButton.Position =
                new Vector2i((int) (CluwneLib.CurrentRenderTarget.Size.X/2f - _message.ClientArea.Width/2f),
                          _message.ClientArea.Bottom() + 20);
            _mainMenuButton.Update(frameTime);
        }

        public override void Render()
        {
            CluwneLib.drawRectangle(0, 0, (int)CluwneLib.CurrentRenderTarget.Size.X,  (int)CluwneLib.CurrentRenderTarget.Size.Y, SFML.Graphics.Color.Black);
            _message.Render();
            _mainMenuButton.Render();
        }

        public override void Dispose()
        {
            _message.Dispose();
            _mainMenuButton.Dispose();
            base.Dispose();
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            _mainMenuButton.MouseDown(e);
            return true;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            _mainMenuButton.MouseUp(e);
            return true;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            _mainMenuButton.MouseMove(e);
        }

        public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            return true;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            _mainMenuButton.KeyDown(e);
            return true;
        }
    }
}