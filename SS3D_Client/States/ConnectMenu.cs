using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using ClientServices;
using ClientServices.Resources;
using SS13.Modules;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS13.UserInterface;
using SS13.Effects;

namespace SS13.States
{
    public class ConnectMenu : State
    {
        #region Fields

        private const float ConnectTimeOut = 5000.0f;

        private StarScroller _starScroller;
        private DateTime _connectTime;
        private Sprite _ss13Text;
        private bool _isConnecting;
        private UiManager _uiManager;

        #endregion

        #region Properties

        public string IpAddress { get; set; }

        #endregion

        #region Startup, Shutdown, Update
        public override bool Startup(Program program)
        {
            Program = program;
            Program.NetworkManager.Disconnect();
            Program.NetworkManager.Connected += OnConnected;
            _starScroller = new StarScroller();

            _uiManager = ServiceManager.Singleton.GetService<UiManager>();
            _ss13Text = ServiceManager.Singleton.GetService<ResourceManager>().GetSpriteFromImage("ss13text");
            _ss13Text.Position = new Vector2D(Gorgon.Screen.Width / 2 - (475 / 2), -140); 
            return true;
        }

        private void OnConnected(Modules.Network.NetworkManager netMgr)
        {
            _isConnecting = false;
            Program.StateManager.RequestStateChange(typeof(LobbyScreen));
        }

        public void StartConnect()
        {
            _connectTime = DateTime.Now;
            _isConnecting = true;
            Program.NetworkManager.ConnectTo(IpAddress);
        }

        public override void Shutdown()
        {
            Program.NetworkManager.Connected -= OnConnected;
            _starScroller = null;
        }

        public override void Update(FrameEventArgs e)
        {
            if (_isConnecting)
            {
                var dif = DateTime.Now - _connectTime;
                if (dif.TotalMilliseconds > ConnectTimeOut)
                {
                    _isConnecting = false;
                    Program.NetworkManager.Disconnect();
                }
            }

            if (_ss13Text.Position.Y < Gorgon.Screen.Height / 2 - 130)
            {
                _ss13Text.Position += new Vector2D(0f, 1 * (float)Gorgon.FrameStats.FrameDrawTime / 20f);
            }

            _uiManager.Update();
        }

        #endregion

        public override void GorgonRender(FrameEventArgs e)
        {
            _starScroller.Render(0,0);
            _ss13Text.Draw();
            _uiManager.Render();
        }
        public override void FormResize()
        {
        }

        #region Input

        public override void KeyDown(KeyboardInputEventArgs e)
        {
            _uiManager.KeyDown(e);
        }
        public override void KeyUp(KeyboardInputEventArgs e)
        {

        }
        public override void MouseUp(MouseInputEventArgs e)
        {
            _uiManager.MouseUp(e);
        }
        public override void MouseDown(MouseInputEventArgs e)
        {
            _uiManager.MouseDown(e);
        }
        public override void MouseMove(MouseInputEventArgs e)
        {
            _uiManager.MouseMove(e);
        }
        public override void MouseWheelMove(MouseInputEventArgs e)
        {
            _uiManager.MouseWheelMove(e);
        }
        #endregion
    }

}
