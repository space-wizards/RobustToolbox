using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Network;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    class AdminPasswordDialog : Window
    {
        private readonly IResourceManager _resourceManager;
        private readonly INetworkManager _networkManager;

        private readonly Textbox _textboxPassword;
        private readonly Button _okayButton;

        public AdminPasswordDialog(Size size, INetworkManager networkManager, IResourceManager resourceManager)
            : base("Admin Login", size, resourceManager)
        {
            _networkManager = networkManager;
            _resourceManager = resourceManager;

            _textboxPassword = new Textbox((int)(size.Width / 2f), _resourceManager);
            _okayButton = new Button("Submit", _resourceManager);
            _okayButton.Clicked += OkayButtonClicked;
            _textboxPassword.OnSubmit += textboxPassword_OnSubmit;
            components.Add(_textboxPassword);
            components.Add(_okayButton);
            Position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(ClientArea.Height / 2f));
        }

        void textboxPassword_OnSubmit(string text)
        {
            if (text.Length > 1 && !string.IsNullOrWhiteSpace(text))
            {
                TryAdminLogin(text);
                _textboxPassword.Text = string.Empty;
            }
        }

        void OkayButtonClicked(Button sender)
        {
            if (_textboxPassword.Text.Length <= 1 || string.IsNullOrWhiteSpace(_textboxPassword.Text)) return;

            TryAdminLogin(_textboxPassword.Text);
            _textboxPassword.Text = string.Empty;
        }

        public override void Update()
        {
            if (disposing || !IsVisible()) return;
            base.Update();
            if (_okayButton == null || _textboxPassword == null) return;

            _okayButton.Position = new Point((int)(Size.Width / 2f) - (int)(_okayButton.ClientArea.Width / 2f), (Size.Height - _okayButton.ClientArea.Height - 5));
            _textboxPassword.Position = new Point((int)(Size.Width / 2f) - (int)(_textboxPassword.ClientArea.Width / 2f),  5);
        }

        private void TryAdminLogin(string password)
        {
            NetOutgoingMessage msg = _networkManager.CreateMessage();
            msg.Write((byte)NetMessage.RequestAdminLogin);
            msg.Write(password);

            _networkManager.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);

            Dispose();
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;
            base.Render();
        }

        public override void Dispose()
        {
            if (disposing) return;
            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (base.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (base.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return;
            base.MouseMove(e);
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (base.KeyDown(e)) return true;
            return false;
        }
    }
}