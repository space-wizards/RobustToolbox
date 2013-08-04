using System;
using System.Drawing;
using System.Windows.Forms;
using ClientInterfaces.Configuration;
using ClientInterfaces.Input;
using ClientInterfaces.Network;
using ClientInterfaces.State;
using ClientInterfaces.UserInterface;
using ClientServices.State.States;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS13.IoC;

namespace SS13
{
    public partial class MainWindow : Form
    {
        #region Fields

        private IConfigurationManager _configurationManager;
        private Input _input;
        private INetworkGrapher _netGrapher;
        private INetworkManager _networkManager;
        private IStateManager _stateManager;
        private IUserInterfaceManager _userInterfaceManager;

        #endregion

        #region Properties

        #endregion

        #region Methods

        #region Constructors

        public MainWindow()
        {
            IoCManager.Resolve<IConfigurationManager>().Initialize("./config.xml");

            InitializeComponent();
        }

        #endregion

        #region EventHandlers

        private void GorgonIdle(object sender, FrameEventArgs e)
        {
            _networkManager.UpdateNetwork();
            _stateManager.Update(e);

            _userInterfaceManager.Update(e.FrameDeltaTime);
            _userInterfaceManager.Render();

            _netGrapher.Update();
        }

        private void MainWindowLoad(object sender, EventArgs e)
        {
            _configurationManager = IoCManager.Resolve<IConfigurationManager>();

            SetupGorgon();
            SetupInput();

            _networkManager = IoCManager.Resolve<INetworkManager>();
            _netGrapher = IoCManager.Resolve<INetworkGrapher>();
            _stateManager = IoCManager.Resolve<IStateManager>();
            _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();

            Gorgon.Go();

            _stateManager.RequestStateChange<ConnectMenu>();
        }

        private void MainWindowResizeEnd(object sender, EventArgs e)
        {
            _input.Mouse.SetPositionRange(0, 0, Gorgon.CurrentClippingViewport.Width,
                                          Gorgon.CurrentClippingViewport.Height);
            _stateManager.CurrentState.FormResize();
        }

        #region Input Handling

        /// <summary>
        /// Handles any keydown events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.KeyboardInputEventArgs"/> instance containing the event data.</param>
        private void KeyDownEvent(object sender, KeyboardInputEventArgs e)
        {
            _stateManager.KeyDown(e);

            switch (e.Key)
            {
                case KeyboardKeys.F3:
                    IoCManager.Resolve<INetworkGrapher>().Toggle();
                    break;
            }
        }

        /// <summary>
        /// Handles any keyup events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.KeyboardInputEventArgs"/> instance containing the event data.</param>
        private void KeyUpEvent(object sender, KeyboardInputEventArgs e)
        {
            _stateManager.KeyUp(e);
        }

        /// <summary>
        /// Handles mouse wheel input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseWheelMoveEvent(object sender, MouseInputEventArgs e)
        {
            _stateManager.MouseWheelMove(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseMoveEvent(object sender, MouseInputEventArgs e)
        {
            _stateManager.MouseMove(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseDownEvent(object sender, MouseInputEventArgs e)
        {
            _stateManager.MouseDown(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseUpEvent(object sender, MouseInputEventArgs e)
        {
            _stateManager.MouseUp(e);
        }

        #endregion

        #endregion

        #region Privates

        private void SetupGorgon()
        {
            uint displayWidth = _configurationManager.GetDisplayWidth();
            uint displayHeight = _configurationManager.GetDisplayHeight();
            bool fullscreen = _configurationManager.GetFullscreen();
            var refresh = (int) _configurationManager.GetDisplayRefresh();
            Size = new Size((int) displayWidth, (int) displayHeight);

            //TODO. Find first compatible videomode and set it if no configuration is present. Else the client might crash due to invalid videomodes on the first start.

            Gorgon.Initialize(true, false);
            //Gorgon.SetMode(this);
            Gorgon.SetMode(this, (int) displayWidth, (int) displayHeight, BackBufferFormats.BufferRGB888, !fullscreen,
                           false, false, refresh);
            Gorgon.AllowBackgroundRendering = true;
            Gorgon.Screen.BackgroundColor = Color.FromArgb(50, 50, 50);
            Gorgon.CurrentClippingViewport = new Viewport(0, 0, Gorgon.Screen.Width, Gorgon.Screen.Height);
            Gorgon.DeviceReset += MainWindowResizeEnd;
            //Gorgon.MinimumFrameTime = PreciseTimer.FpsToMilliseconds(66);
            Gorgon.Idle += GorgonIdle;
        }

        private void SetupInput()
        {
            _input = Input.LoadInputPlugIn(Environment.CurrentDirectory + @"\GorgonInput.DLL", "Gorgon.RawInput");
            _input.Bind(this);

            Cursor.Hide();

            ResizeEnd += MainWindowResizeEnd;

            _input.Mouse.Enabled = true;
            _input.Mouse.Exclusive = false;
            _input.Mouse.AllowBackground = false;
            _input.Mouse.MouseDown += MouseDownEvent;
            _input.Mouse.MouseUp += MouseUpEvent;
            _input.Mouse.MouseMove += MouseMoveEvent;
            _input.Mouse.MouseWheelMove += MouseWheelMoveEvent;

            _input.Keyboard.Enabled = true;
            _input.Keyboard.Exclusive = true;
            _input.Keyboard.KeyDown += KeyDownEvent;
            _input.Keyboard.KeyUp += KeyUpEvent;
            IoCManager.Resolve<IKeyBindingManager>().Initialize(_input.Keyboard);

            _input.Mouse.SetPositionRange(0, 0, Gorgon.CurrentClippingViewport.Width,
                                          Gorgon.CurrentClippingViewport.Height);
        }

        #endregion

        #endregion
    }
}