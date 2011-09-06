using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using SS3D.Modules;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;


namespace SS3D.States
{
    public class ConnectMenu : State
    {
        private SS3D.Effects.StarScroller starScroller;

        private StateManager mStateMgr;
        public string ipTextboxIP = "localhost";
        private bool connecting = false;
        private DateTime connectTime;
        private float connectTimeOut = 5000f;
        private Sprite ss13Text;

        public ConnectMenu()
        {
        }

        #region Startup, Shutdown, Update
        public override bool Startup(Program _prg)
        {
            prg = _prg;
            mStateMgr = prg.mStateMgr;

            prg.mNetworkMgr.Disconnect();
            prg.mNetworkMgr.Connected += new Modules.Network.NetworkStateHandler(mNetworkMgr_Connected);
            starScroller = new Effects.StarScroller();

            ss13Text = ResMgr.Singleton.GetSpriteFromImage("ss13text");
            ss13Text.Position = new Vector2D(Gorgon.Screen.Width / 2 - (475 / 2), -140); 
            return true;
        }

        /*
        void nameTextbox_LostFocus(object sender, EventArgs e)
        {
            ConfigManager.Singleton.Configuration.PlayerName = ((TextBox)sender).Text;
            ConfigManager.Singleton.Save();
        }

        void nameTextbox_Submit(object sender, ValueEventArgs<string> e)
        {
            ConfigManager.Singleton.Configuration.PlayerName = ((TextBox)sender).Text;
            ConfigManager.Singleton.Save();
        }

        private void ipTextBoxChanged(object sender, Miyagi.Common.Events.TextEventArgs e)
        {
            ipTextboxIP = ((TextBox)sender).Text;
        }
        */

        void mNetworkMgr_Connected(Modules.Network.NetworkManager netMgr)
        {
            connecting = false;
            //Send client name
            mStateMgr.RequestStateChange(typeof(LobbyScreen));

        }

        /*
        private void JoinButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            StartConnect();
        }*/

        // called when join button pressed and also if autoconnecting at startup
        public void StartConnect()
        {
            connectTime = DateTime.Now;
            connecting = true;
            prg.mNetworkMgr.ConnectTo(ipTextboxIP);
        }

        public override void Shutdown()
        {
            prg.mNetworkMgr.Connected -= new Modules.Network.NetworkStateHandler(mNetworkMgr_Connected);
            starScroller = null;
        }

        public override void Update(FrameEventArgs e)
        {
            if (connecting)
            {
                TimeSpan dif = DateTime.Now - connectTime;
                if (dif.TotalMilliseconds > connectTimeOut)
                {
                    connecting = false;
                    prg.mNetworkMgr.Disconnect();
                }
            }
            if (ss13Text.Position.Y < Gorgon.Screen.Height / 2 - 130)
            {
                ss13Text.Position += new Vector2D(0f, 1 * (float)Gorgon.FrameStats.FrameDrawTime / 20f);
            }
            
        }

        #endregion

        public override void GorgonRender(FrameEventArgs e)
        {
            starScroller.Render(0,0);
            ss13Text.Draw();

            return;
        }
        public override void FormResize()
        {
        }
        #region Input

        public override void KeyDown(KeyboardInputEventArgs e)
        { }
        public override void KeyUp(KeyboardInputEventArgs e)
        { }
        public override void MouseUp(MouseInputEventArgs e)
        { }
        public override void MouseDown(MouseInputEventArgs e)
        { }
        public override void MouseMove(MouseInputEventArgs e)
        { }
        #endregion
    }

}
