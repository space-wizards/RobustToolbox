using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using SS14.Client.Interfaces.State;
using SS14.Client.Services.UserInterface.Components;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

namespace SS14.Client.Services.State.States
{
    public class MainScreen : State, IState
    {
        #region Fields

        private const float ConnectTimeOut = 5000.0f;
        private readonly List<FloatingDecoration> DecoFloats = new List<FloatingDecoration>();

        private readonly Sprite _background;
        private readonly ImageButton _btnConnect;
        private readonly ImageButton _btnExit;
        private readonly ImageButton _btnOptions;
        private readonly Textbox _txtConnect;
        private readonly Label _lblVersion;
        private readonly SimpleImage _imgTitle;

        private DateTime _connectTime;
        private bool _isConnecting;

        private int _Width;
        private int _Height;

        #endregion

        #region Properties

        #endregion

        public MainScreen(IDictionary<Type, object> managers) : base(managers)
        {
            _Width = (int) CluwneLib.Screen.Size.X;
            _Height = (int) CluwneLib.Screen.Size.Y;
            _background = ResourceManager.GetSprite("coderart");
        

            _btnConnect = new ImageButton
                               {
                                   ImageNormal = "connect_norm",
                                   ImageHover = "connect_hover"
                               };
            _btnConnect.Clicked += _buttConnect_Clicked;

            _btnOptions = new ImageButton
                               {
                                   ImageNormal = "options_norm",
                                   ImageHover = "options_hover"
                               };
            _btnOptions.Clicked += _buttOptions_Clicked;

            _btnExit = new ImageButton
                            {
                                ImageNormal = "exit_norm",
                                ImageHover = "exit_hover"
                            };
            _btnExit.Clicked += _buttExit_Clicked;

            _txtConnect = new Textbox(100, ResourceManager) {Text = ConfigurationManager.GetServerAddress()};
            _txtConnect.Position = new Vector2i(_Width / 3, _Height / 2);
            _txtConnect.OnSubmit += ConnectTextboxOnSubmit;

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            _lblVersion = new Label("v. " + fvi.FileVersion, "CALIBRI", ResourceManager);
            _lblVersion.Text.Color = new SFML.Graphics.Color(245, 245, 245);

            _lblVersion.Position = new Vector2i(_Width -  _lblVersion.ClientArea.Width  - 3,
                                             _Height - _lblVersion.ClientArea.Height - 3);


            _imgTitle = new SimpleImage
            {
                Sprite = "SpaceStationLogoColor",
                Position = new Vector2i(_Width-550, 100),
            };

            _lblVersion.Update(0);
            _imgTitle.Update(0);
            _txtConnect.Position = new Vector2i(_imgTitle.ClientArea.Left + 40, _imgTitle.ClientArea.Bottom() + 50);
            _txtConnect.Update(0);
            _btnConnect.Position = new Vector2i(_txtConnect.Position.X, _txtConnect.ClientArea.Bottom() + 20);
            _btnConnect.Update(0);
            _btnOptions.Position = new Vector2i(_btnConnect.Position.X, _btnConnect.ClientArea.Bottom() + 20);
            _btnOptions.Update(0);
            _btnExit.Position = new Vector2i(_btnOptions.Position.X, _btnOptions.ClientArea.Bottom() + 20);
            _btnExit.Update(0);
        }

        #region IState Members

        public void Render(FrameEventArgs e)
        {
            _background.Draw();
        }

        public void FormResize()
        {
            _Width = (int) CluwneLib.Screen.Size.X;
            _Height = (int) CluwneLib.Screen.Size.Y;
            _lblVersion.Update(0);
            _imgTitle.Update(0);
            _btnExit.Update(0); 
            _btnOptions.Update(0);
            _btnConnect.Update(0);
            _txtConnect.Update(0);

        }	
        #endregion

        #region Input
        public void KeyDown ( KeyEventArgs e )
        {
            UserInterfaceManager.KeyDown(e);
        }

        public void KeyUp ( KeyEventArgs e )
        {
        }

        public void MouseUp ( MouseButtonEventArgs e )
        {
            UserInterfaceManager.MouseUp(e);
        }

        public void MouseDown ( MouseButtonEventArgs e )
        {
            UserInterfaceManager.MouseDown(e);
        }

        public void MouseMoved ( MouseMoveEventArgs e )
        {

        }
        public void MousePressed ( MouseButtonEventArgs e )
        {
            UserInterfaceManager.MouseDown(e);
        }
        public void MouseMove ( MouseMoveEventArgs e )
        {
            UserInterfaceManager.MouseMove(e);
        }

        public void MouseWheelMove ( MouseWheelEventArgs e )
        {
            UserInterfaceManager.MouseWheelMove(e);
        }

        public void MouseEntered(EventArgs e)
        {
            UserInterfaceManager.MouseEntered(e);
        }
        public void MouseLeft(EventArgs e)
        {
            UserInterfaceManager.MouseLeft(e);
        }

        #endregion

        private void _buttExit_Clicked(ImageButton sender)
        {
            CluwneLib.Stop();
        }

        private void _buttOptions_Clicked(ImageButton sender)
        {
            if (_isConnecting)
            {
                _isConnecting = false;
                NetworkManager.Disconnect();
            }

            StateManager.RequestStateChange<OptionsMenu>();
        }

        private void _buttConnect_Clicked(ImageButton sender)
        {
            if (!_isConnecting)
                StartConnect(_txtConnect.Text);
            else
            {
                _isConnecting = false;
                NetworkManager.Disconnect();
            }
        }

        private void ConnectTextboxOnSubmit(string text, Textbox sender)
        {
            StartConnect(text);
        }

        #region Startup, Shutdown, Update

        public void Startup()
        {
            NetworkManager.Disconnect();
            NetworkManager.Connected += OnConnected;
            /*
            DecoFloats.Add(new FloatingDecoration(ResourceManager, "coderart")
                               {
                                   BounceRotate = false,
                                   BounceRotateAngle = 10,
                                   ParallaxScale = 0.001f,
                                   SpriteLocation = new Vector2(0, 0),
                                   Velocity = new Vector2(0, 0),
                                   RotationSpeed = 0.0f
                               });
*/
            // DrawSprite.Axis = new Vector2(DrawSprite.Width / 2f, DrawSprite.Height / 2f);
/*            var clouds = new FloatingDecoration(ResourceManager, "mainbg_clouds")
                             {
                                 BounceRotate = true,
                                 BounceRotateAngle = 10,
                                 ParallaxScale = 0.004f,
                                 SpriteLocation = new Vector2(-50, -50),
                                 Velocity = new Vector2(0, 0),
                                 RotationSpeed = 0.25f,
                             };*/

            //clouds.DrawSprite.Axis = new Vector2(clouds.DrawSprite.Width/2f, clouds.DrawSprite.Height/2f);

           /* DecoFloats.Add(clouds);

            DecoFloats.Add(new FloatingDecoration(ResourceManager, "floating_dude")
                               {
                                   BounceRotate = true,
                                   BounceRotateAngle = 10,
                                   ParallaxScale = 0.005f,
                                   SpriteLocation = new Vector2(125, 115),
                                   Velocity = new Vector2(0, 0),
                                   RotationSpeed = 0.5f
                               });*/

          /*  DecoFloats.Add(new FloatingDecoration(ResourceManager, "floating_oxy")
                               {
                                   BounceRotate = true,
                                   BounceRotateAngle = 15,
                                   ParallaxScale = 0.004f,
                                   SpriteLocation = new Vector2(325, 135),
                                   Velocity = new Vector2(0, 0),
                                   RotationSpeed = -0.60f
                               });*/

/*
            DecoFloats.Add(new FloatingDecoration(ResourceManager, "debris_mid_back")
                               {
                                   BounceRotate = false,
                                   ParallaxScale = 0.003f,
                                   SpriteLocation = new Vector2(450, 400),
                                   Velocity = new Vector2(0, 0),
                                   RotationSpeed = -0.20f
                               });
*/
/*
            DecoFloats.Add(new FloatingDecoration(ResourceManager, "debris_far_right_back")
                               {
                                   BounceRotate = true,
                                   BounceRotateAngle = 20,
                                   ParallaxScale = 0.0032f,
                                   SpriteLocation = new Vector2(Gorgon.Screen.Width - 260, 415),
                                   Velocity = new Vector2(0, 0),
                                   RotationSpeed = 0.1f
                               });*/

/*            DecoFloats.Add(new FloatingDecoration(ResourceManager, "debris_far_right_fore")
                               {
                                   BounceRotate = true,
                                   BounceRotateAngle = 15,
                                   ParallaxScale = 0.018f,
                                   SpriteLocation = new Vector2(Gorgon.Screen.Width - 295, 415),
                                   Velocity = new Vector2(0, 0),
                                   RotationSpeed = -0.36f
                               });*/

/*            DecoFloats.Add(new FloatingDecoration(ResourceManager, "debris_far_left_fore")
                               {
                                   BounceRotate = false,
                                   ParallaxScale = 0.019f,
                                   SpriteLocation = new Vector2(0, 335),
                                   Velocity = new Vector2(6, 2),
                                   RotationSpeed = 0.40f
                               });*/

            foreach (FloatingDecoration floatingDeco in DecoFloats)
                UserInterfaceManager.AddComponent(floatingDeco);

            UserInterfaceManager.AddComponent(_txtConnect);
            UserInterfaceManager.AddComponent(_btnConnect);
            UserInterfaceManager.AddComponent(_btnOptions);
            UserInterfaceManager.AddComponent(_btnExit);
            UserInterfaceManager.AddComponent(_imgTitle);
            UserInterfaceManager.AddComponent(_lblVersion);
        }

        public void Shutdown()
        {
            NetworkManager.Connected -= OnConnected;

            UserInterfaceManager.RemoveComponent(_txtConnect);
            UserInterfaceManager.RemoveComponent(_btnConnect);
            UserInterfaceManager.RemoveComponent(_btnOptions);
            UserInterfaceManager.RemoveComponent(_btnExit);
            UserInterfaceManager.RemoveComponent(_imgTitle);
            UserInterfaceManager.RemoveComponent(_lblVersion);

            foreach (FloatingDecoration floatingDeco in DecoFloats)
                UserInterfaceManager.RemoveComponent(floatingDeco);

            DecoFloats.Clear();
        }

        public void Update(FrameEventArgs e)
        {
            if (_isConnecting)
            {
                TimeSpan dif = DateTime.Now - _connectTime;
                if (dif.TotalMilliseconds > ConnectTimeOut)
                {
                    _isConnecting = false;
                    NetworkManager.Disconnect();
                }
            }
        }

        private void OnConnected(object sender, EventArgs e)
        {
            _isConnecting = false;
            StateManager.RequestStateChange<Lobby>();
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

        #endregion
    }
}