using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using ClientInterfaces.State;
using ClientServices.UserInterface.Components;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;

namespace ClientServices.State.States
{
    public class ConnectMenu : State, IState
    {
        #region Fields

        private const float ConnectTimeOut = 5000.0f;
        private readonly List<FloatingDecoration> DecoFloats = new List<FloatingDecoration>();

        private readonly Sprite _background;
        private readonly ImageButton _butt;
        private readonly ImageButton _buttConnect;
        private readonly ImageButton _buttExit;
        private readonly ImageButton _buttOptions;
        private readonly Textbox _connectTextbox;
        private readonly SimpleImage _glow;
        private readonly Label _lblVersion;
        private readonly SimpleImage _titleImage;

        private DateTime _connectTime;
        private bool _isConnecting;

        #endregion

        #region Properties

        #endregion

        public ConnectMenu(IDictionary<Type, object> managers)
            : base(managers)
        {
            _background = ResourceManager.GetSprite("mainbg_filler");
            _background.Smoothing = Smoothing.Smooth;

            _buttConnect = new ImageButton
                               {
                                   ImageNormal = "connect_norm",
                                   ImageHover = "connect_hover"
                               };
            _buttConnect.Clicked += _buttConnect_Clicked;

            _buttOptions = new ImageButton
                               {
                                   ImageNormal = "options_norm",
                                   ImageHover = "options_hover"
                               };
            _buttOptions.Clicked += _buttOptions_Clicked;

            _buttExit = new ImageButton
                            {
                                ImageNormal = "exit_norm",
                                ImageHover = "exit_hover"
                            };
            _buttExit.Clicked += _buttExit_Clicked;

            _butt = new ImageButton
                        {
                            ImageNormal = "blueprint",
                            ImageHover = "blueprint",
                            Position = new Point(20, 20)
                        };
            _butt.Clicked += butt_Clicked;


            _connectTextbox = new Textbox(100, ResourceManager) {Text = ConfigurationManager.GetServerAddress()};
            _connectTextbox.OnSubmit += ConnectTextboxOnSubmit;
            _connectTextbox.SetVisible(false);

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            _lblVersion = new Label("v. " + fvi.FileVersion, "CALIBRI", ResourceManager);
            _lblVersion.Text.Color = Color.WhiteSmoke;
            _lblVersion.Position = new Point(Gorgon.Screen.Width - _lblVersion.ClientArea.Width - 3,
                                             Gorgon.Screen.Height - _lblVersion.ClientArea.Height - 3);

            _titleImage = new SimpleImage
                              {
                                  Sprite = "SpaceStationLogoColor",
                                  Position = new Point(Gorgon.Screen.Width - 550, 100)
                              };

            _glow = new SimpleImage
                        {
                            Sprite = "mainbg_glow",
                            Position = new Point(0, 0)
                        };
            _glow.size = new Vector2D(Gorgon.Screen.Width, Gorgon.Screen.Height);
        }

        #region IState Members

        public void GorgonRender(FrameEventArgs e)
        {
            _background.Draw(new Rectangle(0, 0, Gorgon.Screen.Width, Gorgon.Screen.Height));
        }

        public void FormResize()
        {
        }

        #endregion

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

        private void butt_Clicked(ImageButton sender)
        {
            StateManager.RequestStateChange<NewLobby>();
        }

        private void _buttExit_Clicked(ImageButton sender)
        {
            Environment.Exit(0);
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
            if (!_connectTextbox.IsVisible())
            {
                _connectTextbox.SetVisible(true);
                return;
            }
            else
            {
                if (!_isConnecting)
                    StartConnect(_connectTextbox.Text);
                else
                {
                    _isConnecting = false;
                    NetworkManager.Disconnect();
                }
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

            DecoFloats.Add(new FloatingDecoration(ResourceManager, "mainbg")
                               {
                                   BounceRotate = false,
                                   BounceRotateAngle = 10,
                                   ParallaxScale = 0.001f,
                                   SpriteLocation = new Vector2D(0, 0),
                                   Velocity = new Vector2D(0, 0),
                                   RotationSpeed = 0.0f
                               });

            //            DrawSprite.Axis = new Vector2D(DrawSprite.Width / 2f, DrawSprite.Height / 2f);
            var clouds = new FloatingDecoration(ResourceManager, "mainbg_clouds")
                             {
                                 BounceRotate = true,
                                 BounceRotateAngle = 10,
                                 ParallaxScale = 0.004f,
                                 SpriteLocation = new Vector2D(-50, -50),
                                 Velocity = new Vector2D(0, 0),
                                 RotationSpeed = 0.25f,
                             };

            //clouds.DrawSprite.Axis = new Vector2D(clouds.DrawSprite.Width/2f, clouds.DrawSprite.Height/2f);

            DecoFloats.Add(clouds);

            DecoFloats.Add(new FloatingDecoration(ResourceManager, "floating_dude")
                               {
                                   BounceRotate = true,
                                   BounceRotateAngle = 10,
                                   ParallaxScale = 0.005f,
                                   SpriteLocation = new Vector2D(125, 115),
                                   Velocity = new Vector2D(0, 0),
                                   RotationSpeed = 0.5f
                               });

            DecoFloats.Add(new FloatingDecoration(ResourceManager, "floating_oxy")
                               {
                                   BounceRotate = true,
                                   BounceRotateAngle = 15,
                                   ParallaxScale = 0.004f,
                                   SpriteLocation = new Vector2D(325, 135),
                                   Velocity = new Vector2D(0, 0),
                                   RotationSpeed = -0.60f
                               });

            DecoFloats.Add(new FloatingDecoration(ResourceManager, "debris_mid_back")
                               {
                                   BounceRotate = false,
                                   ParallaxScale = 0.003f,
                                   SpriteLocation = new Vector2D(450, 400),
                                   Velocity = new Vector2D(0, 0),
                                   RotationSpeed = -0.20f
                               });

            DecoFloats.Add(new FloatingDecoration(ResourceManager, "debris_far_right_back")
                               {
                                   BounceRotate = true,
                                   BounceRotateAngle = 20,
                                   ParallaxScale = 0.0032f,
                                   SpriteLocation = new Vector2D(Gorgon.Screen.Width - 260, 415),
                                   Velocity = new Vector2D(0, 0),
                                   RotationSpeed = 0.1f
                               });

            DecoFloats.Add(new FloatingDecoration(ResourceManager, "debris_far_right_fore")
                               {
                                   BounceRotate = true,
                                   BounceRotateAngle = 15,
                                   ParallaxScale = 0.018f,
                                   SpriteLocation = new Vector2D(Gorgon.Screen.Width - 295, 415),
                                   Velocity = new Vector2D(0, 0),
                                   RotationSpeed = -0.36f
                               });

            DecoFloats.Add(new FloatingDecoration(ResourceManager, "debris_far_left_fore")
                               {
                                   BounceRotate = false,
                                   ParallaxScale = 0.019f,
                                   SpriteLocation = new Vector2D(0, 335),
                                   Velocity = new Vector2D(6, 2),
                                   RotationSpeed = 0.40f
                               });

            foreach (FloatingDecoration floatingDeco in DecoFloats)
                UserInterfaceManager.AddComponent(floatingDeco);

            UserInterfaceManager.AddComponent(_connectTextbox);
            UserInterfaceManager.AddComponent(_buttConnect);
            UserInterfaceManager.AddComponent(_buttOptions);
            UserInterfaceManager.AddComponent(_buttExit);
            UserInterfaceManager.AddComponent(_titleImage);
            UserInterfaceManager.AddComponent(_glow);
            UserInterfaceManager.AddComponent(_lblVersion);
            UserInterfaceManager.AddComponent(_butt);
        }

        public void Shutdown()
        {
            NetworkManager.Connected -= OnConnected;

            UserInterfaceManager.RemoveComponent(_connectTextbox);
            UserInterfaceManager.RemoveComponent(_buttConnect);
            UserInterfaceManager.RemoveComponent(_buttOptions);
            UserInterfaceManager.RemoveComponent(_buttExit);
            UserInterfaceManager.RemoveComponent(_titleImage);
            UserInterfaceManager.RemoveComponent(_glow);
            UserInterfaceManager.RemoveComponent(_lblVersion);
            UserInterfaceManager.RemoveComponent(_butt);

            foreach (FloatingDecoration floatingDeco in DecoFloats)
                UserInterfaceManager.RemoveComponent(floatingDeco);

            DecoFloats.Clear();
        }

        public void Update(FrameEventArgs e)
        {
            _connectTextbox.Position = new Point(_titleImage.ClientArea.Left + 40, _titleImage.ClientArea.Bottom + 50);
            _buttConnect.Position = new Point(_connectTextbox.Position.X, _connectTextbox.ClientArea.Bottom + 20);
            _buttOptions.Position = new Point(_buttConnect.Position.X, _buttConnect.ClientArea.Bottom + 20);
            _buttExit.Position = new Point(_buttOptions.Position.X, _buttOptions.ClientArea.Bottom + 20);

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

        #endregion
    }
}