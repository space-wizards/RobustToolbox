using System;
using System.Collections.Generic;
using ClientInterfaces.State;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using System.Drawing;
using ClientServices.UserInterface.Components;
using Lidgren.Network;

namespace ClientServices.State.States
{
    public class ConnectMenu : State, IState
    {
        #region Fields

        private const float ConnectTimeOut = 5000.0f;

        private readonly Sprite _background;
        private readonly Label _connectButton;
        private readonly Textbox _connectTextbox;
        private readonly Label _optionsButton;
        private readonly Label _exitButton;

        private DateTime _connectTime;
        private bool _isConnecting;

        #endregion

        #region Properties
        #endregion

        public ConnectMenu(IDictionary<Type, object> managers)
            : base(managers)
        {
            _background = ResourceManager.GetSprite("mainbg");
            _background.Smoothing = Smoothing.Smooth;

            _connectButton = new Label("Connect", "CALIBRI", ResourceManager) { DrawBorder = true };
            _connectButton.Clicked += ConnectButtonClicked;

            _optionsButton = new Label("Options", "CALIBRI", ResourceManager) { DrawBorder = true };
            _optionsButton.Clicked += OptionsButtonClicked;

            _exitButton = new Label("Exit", "CALIBRI", ResourceManager) { DrawBorder = true };
            _exitButton.Clicked += ExitButtonClicked;

            _connectTextbox = new Textbox(100, ResourceManager) { Text = ConfigurationManager.GetServerAddress() };
            _connectTextbox.OnSubmit += ConnectTextboxOnSubmit;
        }

        private static void ExitButtonClicked(Label sender)
        {
            Environment.Exit(0);
        }

        private void OptionsButtonClicked(Label sender)
        {
            if (_isConnecting)
            {
                _isConnecting = false;
                NetworkManager.Disconnect();
            }

            StateManager.RequestStateChange<OptionsMenu>();
        }

        private void ConnectButtonClicked(Label sender)
        {
            if (!_isConnecting)
                StartConnect(_connectTextbox.Text);
            else
            {
                _isConnecting = false;
                NetworkManager.Disconnect();
            }
        }

        private void ConnectTextboxOnSubmit(string text)
        {
            StartConnect(text);
        }

        #region Startup, Shutdown, Update
        public void Startup()
        {         
            NetworkManager.Disconnect();
            NetworkManager.Connected += OnConnected;

            UserInterfaceManager.AddComponent(_connectTextbox);
            UserInterfaceManager.AddComponent(_optionsButton);
            UserInterfaceManager.AddComponent(_connectButton);
            UserInterfaceManager.AddComponent(_exitButton);
        }

        private void OnConnected(object sender, EventArgs e)
        {
            _isConnecting = false;
            StateManager.RequestStateChange<LobbyScreen>();
        }

        public void StartConnect(string address)
        {
            if (_isConnecting) return;

            if (NetUtility.Resolve(address) == null)
                throw new InvalidOperationException("Not a valid Address.");

            _connectTime = DateTime.Now;
            _isConnecting = true;
            NetworkManager.ConnectTo(address);
        }

        public void Shutdown()
        {
            NetworkManager.Connected -= OnConnected;

            UserInterfaceManager.RemoveComponent(_connectTextbox);
            UserInterfaceManager.RemoveComponent(_optionsButton);
            UserInterfaceManager.RemoveComponent(_connectButton);
            UserInterfaceManager.RemoveComponent(_exitButton);
        }

        public void Update(FrameEventArgs e)
        {
            _connectTextbox.Position = new Point(Gorgon.Screen.Width - (int)(Gorgon.Screen.Width / 4f) - _connectTextbox.ClientArea.Width, (int)(Gorgon.Screen.Height / 2.7f));
            _connectButton.Position = new Point(_connectTextbox.Position.X, _connectTextbox.Position.Y + _connectTextbox.ClientArea.Height + 2);
            _optionsButton.Position = new Point(_connectButton.Position.X, _connectButton.Position.Y + _connectButton.ClientArea.Height + 10);
            _exitButton.Position = new Point(_optionsButton.Position.X, _optionsButton.Position.Y + _optionsButton.ClientArea.Height + 10);

            _connectButton.Text.Text = _isConnecting ? "Cancel" : "Connect";

            if (_isConnecting)
            {
                var dif = DateTime.Now - _connectTime;
                if (dif.TotalMilliseconds > ConnectTimeOut)
                {
                    _isConnecting = false;
                    NetworkManager.Disconnect();
                }
            }
            UserInterfaceManager.Update();
        }

        #endregion

        public void GorgonRender(FrameEventArgs e)
        {
            _background.Draw(new Rectangle(0, 0, Gorgon.CurrentRenderTarget.Width, Gorgon.CurrentRenderTarget.Height));
            UserInterfaceManager.Render();
        }

        public void FormResize()
        {
        }

        #region Input

        public void KeyDown(KeyboardInputEventArgs e)
        {
            UserInterfaceManager.KeyDown(e);
        }
        public void KeyUp(KeyboardInputEventArgs e)
        {
        }
        public void MouseUp(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseUp(e);
        }
        public void MouseDown(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }
        public void MouseMove(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseMove(e);
        }
        public void MouseWheelMove(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseWheelMove(e);
        }
        #endregion
    }

}
