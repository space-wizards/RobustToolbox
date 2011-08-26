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
        private Star[,] _stars;
        private Random _rnd = new Random();
        private StateManager mStateMgr;
        public string ipTextboxIP = "localhost";
        private string name = "Player";
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
            MakeStars();
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
                ss13Text.Position += new Vector2D(0f, 0.01f);
            }
            
        }
        #endregion

        public override void GorgonRender(FrameEventArgs e)
        {
            Gorgon.Screen.Clear(System.Drawing.Color.Black);
            DrawStars(3, (float)Gorgon.FrameStats.FrameDrawTime / 2000);
            DrawStars(2, (float)Gorgon.FrameStats.FrameDrawTime / 2000);
            for (int layer = 1; layer >= 0; layer--)
                DrawStars(layer, (float)Gorgon.FrameStats.FrameDrawTime / 2000);
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

        #region MY GOD IT'S FULL OF STARS
        private struct Star
        {
            /// <summary>
            /// Position of the star.
            /// </summary>
            public Vector2D Position;
            /// <summary>
            /// Magnitude of the star.
            /// </summary>
            public System.Drawing.Color Magnitude;
            /// <summary>
            /// Vertical delta.
            /// </summary>
            public float VDelta;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="position">Position of the star.</param>
            /// <param name="magnitude">Magnitude of the star.</param>
            public Star(Vector2D position, System.Drawing.Color magnitude)
            {
                Position = position;
                Magnitude = magnitude;
                VDelta = 0;
            }
        }

        private void MakeStars()
        {
            _stars = new Star[64, 4];

            for (int layer = 0; layer < 4; layer++)
            {
                for (int i = 0; i < _stars.Length / 4; i++)
                {
                    _stars[i, layer].Position = new Vector2D((float)(_rnd.NextDouble() * Gorgon.Screen.Width), (float)(_rnd.NextDouble() * Gorgon.Screen.Height));

                    // Select magnitude.
                    switch (layer)
                    {
                        case 0:
                            _stars[i, layer].Magnitude = System.Drawing.Color.FromArgb(255, 255, 255);
                            _stars[i, layer].VDelta = (float)(_rnd.NextDouble() * 100.0) + 55.0f;
                            break;
                        case 1:
                            _stars[i, layer].Magnitude = System.Drawing.Color.FromArgb(192, 192, 192);
                            _stars[i, layer].VDelta = (float)(_rnd.NextDouble() * 50.0) + 27.5f;
                            break;
                        case 2:
                            _stars[i, layer].Magnitude = System.Drawing.Color.FromArgb(128, 128, 128);
                            _stars[i, layer].VDelta = (float)(_rnd.NextDouble() * 25.0) + 13.5f;
                            break;
                        default:
                            _stars[i, layer].Magnitude = System.Drawing.Color.FromArgb(64, 64, 64);
                            _stars[i, layer].VDelta = (float)(_rnd.NextDouble() * 12.5) + 1.0f;
                            break;
                    }
                }
            }
        }

        private void DrawStars(int layer, float deltaTime)
        {
            Gorgon.Screen.BeginDrawing();

            // Draw the stars.
            for (int i = 0; i < _stars.Length / 4; i++)
            {
                Gorgon.Screen.SetPoint((int)_stars[i, layer].Position.X, (int)_stars[i, layer].Position.Y, _stars[i, layer].Magnitude);

                // Move the stars down.
                _stars[i, layer].Position.Y += _stars[i, layer].VDelta * deltaTime;

                // Wrap around.
                if (_stars[i, layer].Position.Y > Gorgon.Screen.Height)
                    _stars[i, layer].Position = new Vector2D((float)(_rnd.NextDouble() * Gorgon.Screen.Width), 0);
            }

            Gorgon.Screen.EndDrawing();
        }

        #endregion

    }

}
