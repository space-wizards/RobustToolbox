using System;
using System.Drawing;
using System.Windows.Forms;
using ClientServices;
using ClientServices.Configuration;
using ClientServices.Resources;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS13.States;
using SS13.Modules;
using SS13.UserInterface;
using ClientServices.Input;

namespace SS13
{
    public partial class MainWindow : Form
    {
        #region Fields

        private readonly Program _program;
        private readonly StateManager _stateManager;
        private Input _input;
        
        #endregion

        #region Properties

        public bool EditMode { get; set; }

        #endregion

        #region Methods

        #region Constructors

        public MainWindow(Program program)
        {
            _program = program;
            _stateManager = _program.StateManager;
            InitializeComponent();
        }

        #endregion

        #region EventHandlers

        private void GorgonIdle(object sender, FrameEventArgs e)
        {
            _program.NetworkManager.UpdateNetwork();

            _stateManager.Update(e);

            ServiceManager.Singleton.GetService<UiManager>().Update();
            ServiceManager.Singleton.GetService<UiManager>().Render();

            _program.NetGrapher.Update();
        }

        private void MainWindowLoad(object sender, EventArgs e)
        {
            SetupGorgon();
            SetupInput();

            // Load Resources.
            ServiceManager.Singleton.Register<ResourceManager>();
            ServiceManager.Singleton.GetService<ResourceManager>().LoadResourceZip(@"..\\..\\..\\Media\\ResourcePack.zip");

            PlayerName_TextBox.Text = ServiceManager.Singleton.GetService<ConfigurationManager>().Configuration.PlayerName;

            Gorgon.Go();

            _stateManager.Startup(typeof(ConnectMenu));
        }

        private void MainWindowResizeEnd(object sender, EventArgs e)
        {
            _input.Mouse.SetPositionRange(0, 0, Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);
            _stateManager.CurrentState.FormResize();
        }

        private void MainWindowFormClosing(object sender, FormClosingEventArgs e)
        {
            if (disconnectToolStripMenuItem.Enabled) { _program.NetworkManager.Disconnect(); }
        }

        private void QuitToolStripMenuItemClick(object sender, EventArgs e)
        {
            Gorgon.Terminate();
            _stateManager.Shutdown();
            Environment.Exit(0);
        }

        private void ToolStripTextBox1KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\r') return;

            ServiceManager.Singleton.GetService<ConfigurationManager>().Configuration.PlayerName = PlayerName_TextBox.Text;
            ServiceManager.Singleton.GetService<ConfigurationManager>().Save();

            ((ConnectMenu)_stateManager.CurrentState).IpAddress = toolStripTextBox1.Text;
            ((ConnectMenu)_stateManager.CurrentState).StartConnect();

            connectToolStripMenuItem.Enabled = false;
            disconnectToolStripMenuItem.Enabled = true;
            menuToolStripMenuItem.HideDropDown();
        }

        private void DisconnectToolStripMenuItemClick(object sender, EventArgs e)
        {
            _program.NetworkManager.Disconnect();
            _stateManager.RequestStateChange(typeof(ConnectMenu));
            connectToolStripMenuItem.Enabled = true;
            disconnectToolStripMenuItem.Enabled = false;
            menuToolStripMenuItem.HideDropDown();
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
            if (e.Position.Y > menuStrip1.Height)
                _stateManager.MouseDown(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseUpEvent(object sender, MouseInputEventArgs e)
        {
            if (e.Position.Y > menuStrip1.Height)
                _stateManager.MouseUp(e);
        }

        #endregion

        #endregion

        #region Privates

        private void SetupGorgon()
        {
            var configuration = ServiceManager.Singleton.GetService<ConfigurationManager>().Configuration;
            Size = new Size((int)configuration.DisplayWidth, (int)configuration.DisplayHeight);

            Gorgon.Initialize(true, false);
            Gorgon.SetMode(this);
            Gorgon.AllowBackgroundRendering = true;
            Gorgon.Screen.BackgroundColor = Color.FromArgb(50, 50, 50);
            Gorgon.CurrentClippingViewport = new Viewport(0, 20, Gorgon.Screen.Width, Gorgon.Screen.Height - 20);

            Gorgon.MinimumFrameTime = PreciseTimer.FpsToMilliseconds(66);
            Gorgon.Idle += new FrameEventHandler(GorgonIdle);
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
            KeyBindingManager.Initialize(_input.Keyboard);

            _input.Mouse.SetPositionRange(0, 0, Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);
        }

        #endregion

        #endregion
    }
}
