using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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

using SS3D.States;
using SS3D.Modules;

namespace SS3D
{
    public partial class MainWindow : Form
    {
        #region Variables.
        private Input _input = null;								// Input devices interface.
        private Mouse _mouse = null;								// Mouse interface.
        private Keyboard _keyboard = null;							// Keyboard interface.
        private RenderImage _backBuffer = null;						// Back buffer.
        private float _radius = 4.0f;								// Pen radius.
        private BlendingModes _blendMode = BlendingModes.Modulated;	// Blend mode.
        private byte[] _backupImage = null;							// Saved image for backup when the render target goes through a mode switch.
        private Joystick _joystick = null;							// Joystick.
        private int _counter = 0;									// Joystick index counter.
        private TextSprite _messageSprite = null;					// Message sprite.

        //Experimental GUI stuff
        private GUISkin _skin;
        private Desktop _desktop;
        private GUIWindow _window;
        //Experimental GUI stuff

        private Modules.StateManager stateMgr;
        private Program prg;

        #endregion

        public MainWindow(Program _prg)
        {
            prg = _prg;
            stateMgr = prg.mStateMgr;
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            Gorgon.Initialize(true, false);

            _input = Input.LoadInputPlugIn(Environment.CurrentDirectory + @"\GorgonInput.DLL", "Gorgon.RawInput");

            // Bind the devices to this window.
            _input.Bind(this);

            // Enable the mouse.
            Cursor = Cursors.Cross;

            this.ResizeEnd += new EventHandler(MainWindow_ResizeEnd);

            _mouse = _input.Mouse;
            _mouse.Enabled = true;
            _mouse.Exclusive = false;
            _mouse.AllowBackground = false;
            _mouse.MouseDown += new MouseInputEvent(MouseDownEvent);
            _mouse.MouseUp += new MouseInputEvent(MouseUpEvent);
            _mouse.MouseMove += new MouseInputEvent(MouseMoveEvent);
            // _mouse.MouseWheelMove += new MouseInputEvent(MouseWheelMove);

            // Enable the keyboard.
            _keyboard = _input.Keyboard;
            _keyboard.Enabled = true;
            _keyboard.Exclusive = true;
            _keyboard.KeyDown += new KeyboardInputEvent(KeyDownEvent);
            _keyboard.KeyUp += new KeyboardInputEvent(KeyUpEvent);

            Cursor.Position = PointToScreen(new Point(100, 100));
            _mouse.SetPosition(100f, 100f);
            //_mouse.SetPositionRange(0, 0, Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);

            Gorgon.SetMode(this);
            Gorgon.Screen.BackgroundColor = Color.FromArgb(50, 50, 50);
            Gorgon.CurrentRenderTarget.AlphaMaskFunction = CompareFunctions.GreaterThan;
            _mouse.SetPositionRange(0, 0, Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);

            Gorgon.Idle += new FrameEventHandler(Gorgon_Idle);

            Gorgon.Go(); //GO MUTHAFUCKA

            ResMgr.Singleton.Initialize();

            _skin = ResMgr.Singleton.GetGuiSkin("Interface1");
            _desktop = new Desktop(_input, _skin);
            _window = new GUIWindow("Window", Gorgon.Screen.Width / 4, Gorgon.Screen.Height / 4, Gorgon.Screen.Width - (Gorgon.Screen.Width / 4) * 2, Gorgon.Screen.Height - (Gorgon.Screen.Height / 4) * 2);
            //_desktop.Windows.Add(_window);
            _window.Text = "This is a GUI window.";

            _desktop.ShowDesktopBackground = false;
            _desktop.BackgroundColor = Drawing.Color.Tan;
            _desktop.FocusRectangleColor = Drawing.Color.FromArgb(128, Drawing.Color.Red);
            _desktop.FocusRectangleBlend = BlendingModes.Additive;
            _desktop.FocusRectangleOutline = false;

            stateMgr.Startup(typeof(ConnectMenu));
        }

        void Gorgon_Idle(object sender, FrameEventArgs e)
        {
            _desktop.Update(e.FrameDeltaTime);
            _desktop.Draw();
        }

        void MainWindow_ResizeEnd(object sender, EventArgs e)
        {
            _mouse.SetPositionRange(0, 0, Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);
            stateMgr.mCurrentState.FormResize();
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
            stateMgr.MouseDown(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseUpEvent(object sender, MouseInputEventArgs e)
        {
            stateMgr.MouseUp(e);
        }

        /// <summary>
        /// Handles the FormClosing event of the MainForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.FormClosingEventArgs"/> instance containing the event data.</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // shutdown networking!!!
            base.OnFormClosing(e);
            Gorgon.Terminate();
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
                ((SS3D.States.ConnectMenu)stateMgr.mCurrentState).ipTextboxIP = toolStripTextBox1.Text;
                ((SS3D.States.ConnectMenu)stateMgr.mCurrentState).StartConnect();
                connectToolStripMenuItem.Enabled = false;
                disconnectToolStripMenuItem.Enabled = true;
                //connectToolStripMenuItem.HideDropDown();
                menuToolStripMenuItem.HideDropDown();
            }
        }


        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            prg.mNetworkMgr.Disconnect();
            stateMgr.RequestStateChange(typeof(SS3D.States.ConnectMenu));
            connectToolStripMenuItem.Enabled = true;
            disconnectToolStripMenuItem.Enabled = false;
            menuToolStripMenuItem.HideDropDown();
        }
    }
}
