using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
        private readonly SimpleImage _titleImage;
        private readonly Label _lblVersion;

        private DateTime _connectTime;
        private bool _isConnecting;

        private List<FloatingDeco> DecoFloats = new List<FloatingDeco>();

        #endregion

        #region Properties
        #endregion

        public ConnectMenu(IDictionary<Type, object> managers)
            : base(managers)
        {
            _background = ResourceManager.GetSprite("mainbg");
            _background.Smoothing = Smoothing.Smooth;

            _connectButton = new Label("Connect", "CALIBRI", ResourceManager) { DrawBorder = true};
            _connectButton.Text.Color = Color.DarkRed;
            _connectButton.Clicked += ConnectButtonClicked;

            _optionsButton = new Label("Options", "CALIBRI", ResourceManager) { DrawBorder = true};
            _optionsButton.Text.Color = Color.DarkRed;
            _optionsButton.Clicked += OptionsButtonClicked;

            _exitButton = new Label("Exit", "CALIBRI", ResourceManager) { DrawBorder = true};
            _exitButton.Text.Color = Color.DarkRed;
            _exitButton.Clicked += ExitButtonClicked;

            _connectTextbox = new Textbox(100, ResourceManager) { Text = ConfigurationManager.GetServerAddress() };
            _connectTextbox.OnSubmit += ConnectTextboxOnSubmit;

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            _lblVersion = new Label("v. " + fvi.FileVersion, "CALIBRI", ResourceManager);
            _lblVersion.Text.Color = Color.WhiteSmoke;
            _lblVersion.Position = new Point(Gorgon.Screen.Width - _lblVersion.ClientArea.Width - 3, Gorgon.Screen.Height - _lblVersion.ClientArea.Height - 3);

            _titleImage = new SimpleImage(ResourceManager, "SpaceStationLogoColor")
                {
                    Position = new Point(Gorgon.Screen.Width - 550, 100)
                };
        }

        private static void ExitButtonClicked(Label sender, MouseInputEventArgs e)
        {
            Environment.Exit(0);
        }

        private void OptionsButtonClicked(Label sender, MouseInputEventArgs e)
        {
            if (_isConnecting)
            {
                _isConnecting = false;
                NetworkManager.Disconnect();
            }

            StateManager.RequestStateChange<OptionsMenu>();
        }

        private void ConnectButtonClicked(Label sender, MouseInputEventArgs e)
        {
            if (!_isConnecting)
                StartConnect(_connectTextbox.Text);
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

            DecoFloats.Add(new FloatingDeco(ResourceManager, "floating_dude")
            {
                BounceRotate = true,
                BounceRotateAngle = 10,
                ParallaxScale = 0.01f,
                SpriteLocation = new Vector2D(125, 115),
                Velocity = new Vector2D(0, 0),
                RotationSpeed = 0.25f
            });

            DecoFloats.Add(new FloatingDeco(ResourceManager, "floating_oxy")
            {
                BounceRotate = true,
                BounceRotateAngle = 15,
                ParallaxScale = 0.008f,
                SpriteLocation = new Vector2D(325, 135),
                Velocity = new Vector2D(0, 0),
                RotationSpeed = -0.30f
            });

            DecoFloats.Add(new FloatingDeco(ResourceManager, "debris_mid_back")
            {
                BounceRotate = false,
                ParallaxScale = 0.001f,
                SpriteLocation = new Vector2D(450, 400),
                Velocity = new Vector2D(0, 0),
                RotationSpeed = -0.10f
            });

            DecoFloats.Add(new FloatingDeco(ResourceManager, "debris_far_right_back")
            {
                BounceRotate = true,
                BounceRotateAngle = 20,
                ParallaxScale = 0.002f,
                SpriteLocation = new Vector2D(Gorgon.Screen.Width - 260, 415),
                Velocity = new Vector2D(0, 0),
                RotationSpeed = 0.05f
            });

            DecoFloats.Add(new FloatingDeco(ResourceManager, "debris_far_right_fore")
            {
                BounceRotate = true,
                BounceRotateAngle = 15,
                ParallaxScale = 0.018f,
                SpriteLocation = new Vector2D(Gorgon.Screen.Width - 295, 415),
                Velocity = new Vector2D(0, 0),
                RotationSpeed = -0.18f
            });

            DecoFloats.Add(new FloatingDeco(ResourceManager, "debris_far_left_fore")
            {
                BounceRotate = false,
                ParallaxScale = 0.019f,
                SpriteLocation = new Vector2D(0, 335),
                Velocity = new Vector2D(3, 1),
                RotationSpeed = 0.20f
            });

            foreach (var floatingDeco in DecoFloats)
                UserInterfaceManager.AddComponent(floatingDeco); 

            UserInterfaceManager.AddComponent(_connectTextbox);
            UserInterfaceManager.AddComponent(_optionsButton);
            UserInterfaceManager.AddComponent(_connectButton);
            UserInterfaceManager.AddComponent(_exitButton);
            UserInterfaceManager.AddComponent(_titleImage);
            UserInterfaceManager.AddComponent(_lblVersion);

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
            UserInterfaceManager.RemoveComponent(_titleImage);
            UserInterfaceManager.RemoveComponent(_lblVersion);

            foreach (var floatingDeco in DecoFloats)
                UserInterfaceManager.RemoveComponent(floatingDeco);

            DecoFloats.Clear();
        }

        public void Update(FrameEventArgs e)
        {
            _connectTextbox.Position = new Point(Gorgon.CurrentClippingViewport.Width - (int)(Gorgon.CurrentClippingViewport.Width / 4f) - _connectTextbox.ClientArea.Width, (int)(Gorgon.CurrentClippingViewport.Height / 2.7f));
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
            UserInterfaceManager.Update(e.FrameDeltaTime);
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
