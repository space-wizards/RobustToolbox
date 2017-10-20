using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.State.States;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    public class DisconnectedScreenBlocker : Control
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

            _message = new Label(message, "CALIBRI");
            _mainMenuButton = new Button("Main Menu");
            _mainMenuButton.Clicked += MainMenuButtonClicked;
            _mainMenuButton.Label.FillColor = new Color(245, 245, 245);
            _message.ForegroundColor = new Color(245, 245, 245);
        }

        private void MainMenuButtonClicked(Button sender)
        {
            _stateManager.RequestStateChange<MainScreen>();
        }

        protected override void OnCalcRect()
        {
            throw new System.NotImplementedException();
        }

        public override void Update(float frameTime)
        {
            _message.Position = new Vector2i((int)(CluwneLib.CurrentRenderTarget.Size.X / 2f - _message.ClientArea.Width / 2f),
                                          (int)(CluwneLib.CurrentRenderTarget.Size.Y / 2f - _message.ClientArea.Height / 2f) -
                                          50);
            _message.Update(frameTime);
            _mainMenuButton.Position =
                new Vector2i((int)(CluwneLib.CurrentRenderTarget.Size.X / 2f - _message.ClientArea.Width / 2f),
                          _message.ClientArea.Bottom + 20);
            _mainMenuButton.Update(frameTime);
        }

        public override void Draw()
        {
            CluwneLib.drawRectangle(0, 0, (int)CluwneLib.CurrentRenderTarget.Size.X, (int)CluwneLib.CurrentRenderTarget.Size.Y, Color4.Black);
            _message.Draw();
            _mainMenuButton.Draw();
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

        public override bool MouseWheelMove(MouseWheelScrollEventArgs e)
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
