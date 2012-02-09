using System;
using System.Collections.Generic;
using ClientInterfaces.State;
using ClientServices.Helpers;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.State.States
{
    public class ConnectMenu : State, IState
    {
        #region Fields

        private const float ConnectTimeOut = 5000.0f;

        private StarScroller _starScroller;
        private DateTime _connectTime;
        private Sprite _ss13Text;
        private bool _isConnecting;

        #endregion

        #region Properties

        public string IpAddress { get; set; }

        #endregion

        public ConnectMenu(IDictionary<Type, object> managers)
            : base(managers)
        {
            
        }

        #region Startup, Shutdown, Update
        public void Startup()
        {
            NetworkManager.Disconnect();
            NetworkManager.Connected += OnConnected;

            _starScroller = new StarScroller();
            _ss13Text = ResourceManager.GetSpriteFromImage("ss13text");
            _ss13Text.Position = new Vector2D(Gorgon.Screen.Width / 2 - (475 / 2), -140); 
        }

        private void OnConnected(object sender, EventArgs e)
        {
            _isConnecting = false;
            StateManager.RequestStateChange<LobbyScreen>();
        }

        public void StartConnect()
        {
            _connectTime = DateTime.Now;
            _isConnecting = true;
            NetworkManager.ConnectTo(IpAddress);
        }

        public void Shutdown()
        {
            NetworkManager.Connected -= OnConnected;
            _starScroller = null;
        }

        public void Update(FrameEventArgs e)
        {
            if (_isConnecting)
            {
                var dif = DateTime.Now - _connectTime;
                if (dif.TotalMilliseconds > ConnectTimeOut)
                {
                    _isConnecting = false;
                    NetworkManager.Disconnect();
                }
            }

            if (_ss13Text.Position.Y < Gorgon.Screen.Height / 2 - 130)
            {
                _ss13Text.Position += new Vector2D(0f, 1 * (float)Gorgon.FrameStats.FrameDrawTime / 20f);
            }

            UserInterfaceManager.Update();
        }

        #endregion

        public void GorgonRender(FrameEventArgs e)
        {
            _starScroller.Render(0,0);
            _ss13Text.Draw();
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
