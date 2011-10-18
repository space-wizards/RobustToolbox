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

using SS3D.States;
using SS3D.Modules;
using SS3D.UserInterface;

using Lidgren.Network;

using CGO;
using System.Security;
using System.Security.Permissions;
using ClientServices.Input;
using ClientConfigManager;
using ClientResourceManager;
using ClientServices.Map;
using ClientServices.Map.Tiles;

namespace SS3D
{
    public partial class MainWindow : Form
    {
        #region Variables.
        private Input _input = null;								// Input devices interface.
        private Mouse _mouse = null;								// Mouse interface.
        private Keyboard _keyboard = null;							// Keyboard interface.

        //Experimental GUI stuff
        private GUISkin _skin;
        private UIDesktop _desktop;

        private Modules.StateManager stateMgr;
        private Program prg;
        private AlignmentOptions alignType = AlignmentOptions.AlignTile;
        private Type atomSpawnType = null;
        private Type tileSpawnType;
        public bool editMode = false;
        public Dictionary<string, Type> atomTypes;

        #endregion

        public MainWindow(Program _prg)
        {
            prg = _prg;
            stateMgr = prg.mStateMgr;
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            SetupGorgon();
            SetupInput();
            ResMgr.Singleton.Initialize();
            SetupUserInterface();
            SetupEditMenu();

            PlayerName_TextBox.Text = ConfigManager.Singleton.Configuration.PlayerName;

            Gorgon.Go(); //GO MUTHAFUCKA

            stateMgr.Startup(typeof(ConnectMenu));
        }
        
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
            Cursor = Cursors.Cross;

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
            _skin = ResMgr.Singleton.GetGuiSkin("Interface1");
            UIDesktop.Initialize(_input, _skin);
            _desktop = UIDesktop.Singleton;
            _desktop.ShowDesktopBackground = false;
            _desktop.BackgroundColor = Drawing.Color.Black;
            _desktop.FocusRectangleColor = Drawing.Color.FromArgb(128, Drawing.Color.Red);
            _desktop.FocusRectangleBlend = BlendingModes.Additive;
            _desktop.FocusRectangleOutline = false;

            ClientServices.ServiceManager.Singleton.AddService(UiManager.Singleton);
        }

        public void SetupEditMenu()
        {
            atomTypes = new Dictionary<string, Type>();
            Type[] typeList = GetTypes();
            for (int i = 0; i < typeList.Length; i++)
            {
                atomTypes.Add(typeList[i].Name, typeList[i]);
            }
            PopulateEditMenu();
        }

        private Type[] GetTypes()
        {
            Assembly ass = Assembly.GetExecutingAssembly(); //LOL ASS
            return ass.GetTypes().Where(t => t.IsSubclassOf(typeof(Atom.Atom))).ToArray();
        }


        #region Trees are fucked
        // Trees seem to be fucked right now as they always steal focus so this is currently unusued.
        /*private void PopulateTreeView()
        {
            treeView1.BeginUpdate();
            foreach (Type t in atomTypes.Values)
            {
                if (t.IsAbstract && t.BaseType == typeof(Atom.Atom))
                {
                    TreeNode[] array = GetChildren(t);
                    treeView1.Nodes.Add(new TreeNode(t.Name, array));
                }
            }
            treeView1.EndUpdate();
        }*/

        /*private TreeNode[] GetChildren(Type t)
        {
            List<TreeNode> nodes = new List<TreeNode>();

            foreach (Type type in atomTypes.Values)
            {
                if (type.IsAbstract && type.BaseType == t)
                {
                    TreeNode[] array = GetChildren(type);
                    nodes.Add(new TreeNode(type.Name, array));
                }
                else if (!type.IsAbstract && type.BaseType == t)
                {
                    nodes.Add(new TreeNode(type.Name));
                }
            }

            return nodes.ToArray();
        }*/
        #endregion
        

        void Gorgon_Idle(object sender, FrameEventArgs e)
        {
            // Update networking
            prg.mNetworkMgr.UpdateNetwork();


            // Update the state manager - this will update the active state.
            prg.mStateMgr.Update(e);

            //Update the other NEW GUI shit.
            UiManager.Singleton.Update();
            UiManager.Singleton.Render();

            //Update GUI shit
            _desktop.Update(e.FrameDeltaTime);
            _desktop.Draw();

            prg.NetGrapher.Update();
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
            if(e.Position.Y > menuStrip1.Height || !editToolStripMenuItem.Selected)
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
                ConfigManager.Singleton.Configuration.PlayerName = PlayerName_TextBox.Text;
                ConfigManager.Singleton.Save();
                ((SS3D.States.ConnectMenu)stateMgr.mCurrentState).ipTextboxIP = toolStripTextBox1.Text;
                ((SS3D.States.ConnectMenu)stateMgr.mCurrentState).StartConnect();
                connectToolStripMenuItem.Enabled = false;
                disconnectToolStripMenuItem.Enabled = true;
                editModeToolStripMenuItem.Enabled = true;
                menuToolStripMenuItem.HideDropDown();
            }
        }
        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            prg.mNetworkMgr.Disconnect();
            stateMgr.RequestStateChange(typeof(SS3D.States.ConnectMenu));
            connectToolStripMenuItem.Enabled = true;
            disconnectToolStripMenuItem.Enabled = false;
            editModeToolStripMenuItem.Enabled = false;
            editToolStripMenuItem.Enabled = false;
            menuToolStripMenuItem.HideDropDown();
        }

        private void editModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (stateMgr.prg.mNetworkMgr.isConnected)
            {
                editToolStripMenuItem.Enabled = !editToolStripMenuItem.Enabled;
                editToolStripMenuItem.Visible = !editToolStripMenuItem.Visible;
                editMode = !editMode;
                statusStrip1.Visible = !statusStrip1.Visible;
            }
        }



        public Type GetAtomSpawnType()
        {
            return atomSpawnType;
        }

        public Type GetTileSpawnType()
        {
            return tileSpawnType;
        }

        #region Edit menu
        #region Atoms
        public void PopulateEditMenu()
        {
            atomToolStripMenuItem.DropDownItems.Clear();
            List<ToolStripMenuItem> items = new List<ToolStripMenuItem>();
            foreach (Type t in atomTypes.Values)
            {
                if (t.IsAbstract && t.BaseType == typeof(Atom.Atom))
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(t.Name);
                    ToolStripMenuItem[] itemItems = GetChildren(t).ToArray();
                    item.DropDownItems.AddRange(itemItems);
                    item.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
                    item.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                    items.Add(item);
                }
            }
            atomToolStripMenuItem.DropDownItems.AddRange(items.ToArray());
        }

        private List<ToolStripMenuItem> GetChildren(Type t)
        {
            List<ToolStripMenuItem> menuItems = new List<ToolStripMenuItem>();
            foreach (Type type in atomTypes.Values)
            {
                if (type.IsAbstract && type.BaseType == t)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(type.Name);
                    ToolStripMenuItem[] itemItems = GetChildren(type).ToArray();
                    item.DropDownItems.AddRange(itemItems);
                    item.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
                    item.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                    menuItems.Add(item);
                }
                else if (!type.IsAbstract && type.BaseType == t)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(type.Name);
                    item.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
                    item.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                    item.Click += new EventHandler(atomMenu_Click);
                    menuItems.Add(item);
                }
            }
            return menuItems;
        }

        private void noneToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            tileSpawnType = null;
            atomSpawnType = null;
            toolStripStatusLabel1.Text = "Right click to delete an atom";
        }

        private void atomMenu_Click(object sender, EventArgs e)
        {
            tileSpawnType = null;
            if (atomTypes.ContainsKey(((ToolStripDropDownItem)sender).Text))
            {
                atomSpawnType = atomTypes[((ToolStripDropDownItem)sender).Text];
                //PlacementManager.Singleton.SendObjectRequestEDITMODE(atomSpawnType, alignType);
                toolStripStatusLabel1.Text = atomSpawnType.Name.ToString();
            }
            else
            {
                atomSpawnType = null;
                toolStripStatusLabel1.Text = "Error: Atom '" + ((ToolStripDropDownItem)sender).Text + "' not found!";
            }
        }

        private void toolStripTextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                tileSpawnType = null;
                foreach (Type t in atomTypes.Values)
                {
                    if (t.Name == toolStripTextBox2.Text)
                    {
                        atomSpawnType = t;
                        toolStripStatusLabel1.Text = atomSpawnType.ToString();
                        break;
                    }
                }
            }
        }
        #endregion
        #region Tiles
        private void turfToolStripMenuItem_Click(object sender, EventArgs e)
        {
            atomSpawnType = null;
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

        private void PlayerName_TextBox_Click(object sender, EventArgs e)
        {

        }
        #endregion

        private void toggleEditToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NetOutgoingMessage message = prg.mNetworkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.EDITMODE_ToggleEditMode);
            prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        private void wallToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            alignType = AlignmentOptions.AlignWall;
        }

        private void tileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            alignType = AlignmentOptions.AlignTile;
        }

        private void sameTypeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            alignType = AlignmentOptions.AlignSimilar;
        }

        private void freeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            alignType = AlignmentOptions.AlignNone;
        }
    }
}
