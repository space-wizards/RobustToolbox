using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.State;
using ClientInterfaces.UserInterface;
using ClientServices.State.States;
using GorgonLibrary;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    public class DisconnectedScreenBlocker : GuiComponent
    {
        private readonly IStateManager _stateManager;
        private readonly IUserInterfaceManager _userInterfaceManager;
        private readonly IResourceManager _resourceManager;

        private readonly Label _message;
        private readonly Button _mainMenuButton;

        public DisconnectedScreenBlocker(IStateManager stateManager, IUserInterfaceManager userInterfaceManager, IResourceManager resourceManager, string message = "Connection closed.")
        {
            _stateManager = stateManager;
            _resourceManager = resourceManager;
            _userInterfaceManager = userInterfaceManager;
            _userInterfaceManager.DisposeAllComponents();

            _message = new Label(message, "CALIBRI", _resourceManager);
            _mainMenuButton = new Button("Main Menu", _resourceManager);
            _mainMenuButton.Clicked += MainMenuButtonClicked;
            _mainMenuButton.Label.Color = Color.WhiteSmoke;
            _message.Text.Color = Color.WhiteSmoke;
        }

        void MainMenuButtonClicked(Button sender)
        {
            _stateManager.RequestStateChange<ConnectMenu>();
        }

        public override void Update()
        {
            _message.Position = new Point((int)(Gorgon.Screen.Width / 2f - _message.ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f - _message.ClientArea.Height / 2f) - 50);
            _message.Update();
            _mainMenuButton.Position = new Point((int)(Gorgon.Screen.Width / 2f - _message.ClientArea.Width / 2f), _message.ClientArea.Bottom + 20);
            _mainMenuButton.Update();
        }

        public override void Render()
        {
            Gorgon.Screen.FilledRectangle(0,0,Gorgon.Screen.Width, Gorgon.Screen.Height, Color.Black);
            _message.Render();
            _mainMenuButton.Render();
        }

        public override void Dispose()
        {
            _message.Dispose();
            _mainMenuButton.Dispose();
            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            _mainMenuButton.MouseDown(e);
            return true;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            _mainMenuButton.MouseUp(e);
            return true;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            _mainMenuButton.MouseMove(e);
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            return true;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            _mainMenuButton.KeyDown(e);
            return true;
        }
    }
}
