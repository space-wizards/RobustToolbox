using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows.Forms;

using System.IO;
using System.Xml;
using System.Xml.Serialization;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.FileSystems;
using GorgonLibrary.Framework;
using GorgonLibrary.GUI;
using GorgonLibrary.Graphics.Utilities;

using Drawing = System.Drawing;

using SS13.States;
using SS13.Modules;
using SS13.UserInterface;

using Lidgren.Network;

using CGO;
using System.Security;
using System.Security.Permissions;
using ClientServices.Input;
using ClientConfigManager;
using ClientResourceManager;
using ClientServices.Map;
using ClientServices.Map.Tiles;

namespace SS13
{
    public partial class MainWindow : Form
    {
        #region Fields
        private Input _input = null;								// Input devices interface.
        private Mouse _mouse = null;								// Mouse interface.
        private Keyboard _keyboard = null;							// Keyboard interface.
        private StateManager stateMgr;
        private Program prg;
        private PlacementOption alignType = PlacementOption.AlignNone;
        private Type tileSpawnType;
        public bool editMode = false;
        #endregion

        #region Properties
        public Type GetTileSpawnType()
        {
            return tileSpawnType;
        }
        #endregion

        #region Methods
        #region Constructors
        public MainWindow(Program _prg)
        {
            prg = _prg;
            stateMgr = prg.mStateMgr;
            InitializeComponent();
        }
        #endregion

        #region EventHandlers
        private void Gorgon_Idle(object sender, FrameEventArgs e)
        {
            // Update networking
            prg.mNetworkMgr.UpdateNetwork();

            // Update the state manager - this will update the active state.
            prg.mStateMgr.Update(e);

            //Update the other NEW GUI shit.
            UiManager.Singleton.Update();
            UiManager.Singleton.Render();

            prg.NetGrapher.Update();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            SetupGorgon();
            SetupInput();

            ResMgr.Singleton.LoadResourceZip(@"..\\..\\..\\Media\\ResourcePack.zip");

            SetupUserInterface();

            PlayerName_TextBox.Text = ConfigManager.Singleton.Configuration.PlayerName;

            Gorgon.Go(); //GO MUTHAFUCKA

            stateMgr.Startup(typeof(ConnectMenu));
        }

        void MainWindow_ResizeEnd(object sender, EventArgs e)
        {
            _mouse.SetPositionRange(0, 0, Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);
            stateMgr.mCurrentState.FormResize();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (disconnectToolStripMenuItem.Enabled) { prg.mNetworkMgr.Disconnect(); }
        }

        /// <summary>
        /// Handles any keydown events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.KeyboardInputEventArgs"/> instance containing the event data.</param>
        private void KeyDownEvent(object sender, KeyboardInputEventArgs e)
        {
            stateMgr.KeyDown(e);
        }

        /// <summary>
        /// Handles any keyup events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.KeyboardInputEventArgs"/> instance containing the event data.</param>
        private void KeyUpEvent(object sender, KeyboardInputEventArgs e)
        {
            stateMgr.KeyUp(e);
        }

        /// <summary>
        /// Handles mouse wheel input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseWheelMoveEvent(object sender, MouseInputEventArgs e)
        {
            stateMgr.MouseWheelMove(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseMoveEvent(object sender, MouseInputEventArgs e)
        {
            stateMgr.MouseMove(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseDownEvent(object sender, MouseInputEventArgs e)
        {
            if (e.Position.Y > menuStrip1.Height)
                stateMgr.MouseDown(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseUpEvent(object sender, MouseInputEventArgs e)
        {
            if (e.Position.Y > menuStrip1.Height)
                stateMgr.MouseUp(e);
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Gorgon.Terminate();
            stateMgr.Shutdown();
            Environment.Exit(0);
        }

        private void toolStripTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                ConfigManager.Singleton.Configuration.PlayerName = PlayerName_TextBox.Text;
                ConfigManager.Singleton.Save();
                ((SS13.States.ConnectMenu)stateMgr.mCurrentState).ipTextboxIP = toolStripTextBox1.Text;
                ((SS13.States.ConnectMenu)stateMgr.mCurrentState).StartConnect();
                connectToolStripMenuItem.Enabled = false;
                disconnectToolStripMenuItem.Enabled = true;
                menuToolStripMenuItem.HideDropDown();
            }
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            prg.mNetworkMgr.Disconnect();
            stateMgr.RequestStateChange(typeof(SS13.States.ConnectMenu));
            connectToolStripMenuItem.Enabled = true;
            disconnectToolStripMenuItem.Enabled = false;
            menuToolStripMenuItem.HideDropDown();
        }
        #endregion

        #region Private
        private void SetupGorgon()
        {
            this.Size = new Drawing.Size((int)ConfigManager.Singleton.Configuration.DisplayWidth, (int)ConfigManager.Singleton.Configuration.DisplayHeight);

            Gorgon.Initialize(true, false);
            Gorgon.SetMode(this);
            Gorgon.AllowBackgroundRendering = true;
            Gorgon.Screen.BackgroundColor = Color.FromArgb(50, 50, 50);

            Gorgon.CurrentClippingViewport = new Viewport(0, 20, Gorgon.Screen.Width, Gorgon.Screen.Height - 20);
            PreciseTimer preciseTimer = new PreciseTimer();
            Gorgon.MinimumFrameTime = PreciseTimer.FpsToMilliseconds(66);
            Gorgon.Idle += new FrameEventHandler(Gorgon_Idle);
        }

        private void SetupInput()
        {
            _input = Input.LoadInputPlugIn(Environment.CurrentDirectory + @"\GorgonInput.DLL", "Gorgon.RawInput");

            // Bind the devices to this window.
            _input.Bind(this);

            // Enable the mouse.
            //Cursor = Cursors.Cross;
            Cursor.Hide();

            this.ResizeEnd += new EventHandler(MainWindow_ResizeEnd);

            _mouse = _input.Mouse;
            _mouse.Enabled = true;
            _mouse.Exclusive = false;
            _mouse.AllowBackground = false;
            _mouse.MouseDown += new MouseInputEvent(MouseDownEvent);
            _mouse.MouseUp += new MouseInputEvent(MouseUpEvent);
            _mouse.MouseMove += new MouseInputEvent(MouseMoveEvent);
            _mouse.MouseWheelMove += new MouseInputEvent(MouseWheelMoveEvent);

            // Enable the keyboard.
            _keyboard = _input.Keyboard;
            _keyboard.Enabled = true;
            _keyboard.Exclusive = true;
            _keyboard.KeyDown += new KeyboardInputEvent(KeyDownEvent);
            _keyboard.KeyUp += new KeyboardInputEvent(KeyUpEvent);
            KeyBindingManager.Initialize(_keyboard);

            _mouse.SetPositionRange(0, 0, Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);
        }

        private void SetupUserInterface()
        {
            ClientServices.ServiceManager.Singleton.AddService(UiManager.Singleton);
        }
        #endregion

        #region Public
        #endregion
        #endregion

        #region Tiles
        private void turfToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tileSpawnType = typeof(ClientServices.Map.Tiles.Floor.Floor);
            toolStripStatusLabel1.Text = tileSpawnType.ToString();
        }

        private void spaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tileSpawnType = typeof(ClientServices.Map.Tiles.Floor.Space);
            //PlacementManager.Singleton.SendObjectRequestEDITMODE(tileSpawnType, AlignmentOptions.AlignTile);
        }

        private void floorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tileSpawnType = typeof(ClientServices.Map.Tiles.Floor.Floor);
            //PlacementManager.Singleton.SendObjectRequestEDITMODE(tileSpawnType, AlignmentOptions.AlignTile);
        }

        private void wallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tileSpawnType = typeof(ClientServices.Map.Tiles.Wall.Wall);
            //PlacementManager.Singleton.SendObjectRequestEDITMODE(tileSpawnType, AlignmentOptions.AlignTile);
        }

        private void noneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tileSpawnType = null;
        }
        #endregion
    }
}
