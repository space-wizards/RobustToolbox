using System;
using System.Collections.Generic;
using ClientInterfaces.State;
using ClientServices.Helpers;
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

        private DateTime _connectTime;
        private bool _isConnecting;
        private Sprite _background;

        private readonly Label _connectbtt;
        private readonly Textbox _connecttxt;

        private readonly Label _optionsbtt;

        private readonly Label _exitbtt;

        #endregion

        #region Properties
        #endregion

        public ConnectMenu(IDictionary<Type, object> managers)
            : base(managers)
        {
            _background = ResourceManager.GetSprite("mainbg");
            _background.Smoothing = Smoothing.Smooth;

            _connectbtt = new Label("Connect", "CALIBRI", ResourceManager);
            _connectbtt.DrawBorder = true;
            _connectbtt.Clicked += new Label.LabelPressHandler(_connectbtt_Clicked);

            _optionsbtt = new Label("Options", "CALIBRI", ResourceManager);
            _optionsbtt.DrawBorder = true;
            _optionsbtt.Clicked += new Label.LabelPressHandler(_optionsbtt_Clicked);

            _exitbtt = new Label("Exit", "CALIBRI", ResourceManager);
            _exitbtt.DrawBorder = true;
            _exitbtt.Clicked += new Label.LabelPressHandler(_exitbtt_Clicked);

            _connecttxt = new Textbox(100, ResourceManager);
            _connecttxt.OnSubmit += new Textbox.TextSubmitHandler(_connecttxt_OnSubmit);
            _connecttxt.Text = "localhost";
        }

        void _exitbtt_Clicked(Label sender)
        {
            Environment.Exit(0);
        }

        void _optionsbtt_Clicked(Label sender)
        {
            if (_isConnecting)
            {
                _isConnecting = false;
                NetworkManager.Disconnect();
            }

            StateManager.RequestStateChange<OptionsMenu>();
        }

        void _connectbtt_Clicked(Label sender)
        {
            if (!_isConnecting)
                StartConnect(_connecttxt.Text);
            else
            {
                _isConnecting = false;
                NetworkManager.Disconnect();
            }
        }

        void _connecttxt_OnSubmit(string text)
        {
            StartConnect(text);
        }

        #region Startup, Shutdown, Update
        public void Startup()
        {         
            NetworkManager.Disconnect();
            NetworkManager.Connected += OnConnected;

            UserInterfaceManager.AddComponent(_connecttxt);
            UserInterfaceManager.AddComponent(_optionsbtt);
            UserInterfaceManager.AddComponent(_connectbtt);
            UserInterfaceManager.AddComponent(_exitbtt);
        }

        private void OnConnected(object sender, EventArgs e)
        {
            _isConnecting = false;
            StateManager.RequestStateChange<LobbyScreen>();
        }

        public void StartConnect(string IP)
        {
            if (!_isConnecting)
            {
                if (NetUtility.Resolve(IP) == null)
                    throw new InvalidOperationException("Not a valid Address.");

                _connectTime = DateTime.Now;
                _isConnecting = true;
                NetworkManager.ConnectTo(IP);
            }
        }

        public void Shutdown()
        {
            NetworkManager.Connected -= OnConnected;

            UserInterfaceManager.RemoveComponent(_connecttxt);
            UserInterfaceManager.RemoveComponent(_optionsbtt);
            UserInterfaceManager.RemoveComponent(_connectbtt);
            UserInterfaceManager.RemoveComponent(_exitbtt);
        }

        public void Update(FrameEventArgs e)
        {
            _connecttxt.Position = new Point(Gorgon.Screen.Width - (int)(Gorgon.Screen.Width / 4f) - _connecttxt.ClientArea.Width, (int)(Gorgon.Screen.Height / 2.7f));
            _connectbtt.Position = new Point(_connecttxt.Position.X, _connecttxt.Position.Y + _connecttxt.ClientArea.Height + 2);
            _optionsbtt.Position = new Point(_connectbtt.Position.X, _connectbtt.Position.Y + _connectbtt.ClientArea.Height + 10);
            _exitbtt.Position = new Point(_optionsbtt.Position.X, _optionsbtt.Position.Y + _optionsbtt.ClientArea.Height + 10);

            _connectbtt.Text.Text = _isConnecting ? "Cancel" : "Connect";

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
