using System;
using System.IO;
using System.Linq;

using Lidgren.Network;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Network;
using SS3D.Modules.UI;
using SS3D.Atom;

using SS3D_shared;

using System.Collections.Generic;
using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using System.Windows.Forms;

namespace SS3D.States
{
    public class GameScreen : State
    {
        #region Variables
        private StateManager mStateMgr;
        public Map map;
        private AtomManager atomManager;
        private GameInterfaceManager gameInterface;

        //private GUI guiGameScreen;
        private Chatbox gameChat;
        private ushort defaultChannel;
        public PlayerController playerController;
        public DateTime lastUpdate;
        public DateTime now;
        private RenderImage baseTarget;

        private List<Light> lightsLastFrame = new List<Light>();
        private List<Light> lightsThisFrame = new List<Light>();

        private int screenWidthTiles = 15; // How many tiles around us do we draw?
        private int screenHeightTiles = 12;

        private float realScreenWidthTiles = 0;
        private float realScreenHeightTiles = 0;

        private bool showStats = false;     // show FPS etc. panel if true
        private bool showDebug = false;     // show AABBs & Bounding Circles on atoms.
        //private Label fpsLabel1, fpsLabel2, fpsLabel3, fpsLabel4;

        public float xTopLeft { get; private set; }
        public float yTopLeft { get; private set; }

        private float scaleX = 1.0f;
        private float scaleY = 1.0f;

        private System.Drawing.Point screenSize;
   
        #region Mouse/Camera stuff
        private DateTime lastRMBClick = DateTime.Now;
        private int lastMouseX = 0;
        private int lastMouseY = 0;

        public Vector2D mousePosScreen = Vector2D.Zero;
        public Vector2D mousePosWorld = Vector2D.Zero;

        #endregion

        #endregion

        public GameScreen()
        {
        }

        #region Startup, Shutdown, Update
        public override bool Startup(Program _prg)
        {
            prg = _prg;
            mStateMgr = prg.mStateMgr;

            lastUpdate = DateTime.Now;
            now = DateTime.Now;

            defaultChannel = 1;

            map = new Map();

            atomManager = new AtomManager(this, prg);
            playerController = new PlayerController(this, atomManager);
            //SetUp();
            //SetUpGUI();

            prg.mNetworkMgr.MessageArrived += new NetworkMsgHandler(mNetworkMgr_MessageArrived);

            prg.mNetworkMgr.SetMap(map);
            prg.mNetworkMgr.RequestMap();

            //TODO This should go somewhere else, there should be explicit session setup and teardown at some point.
            prg.mNetworkMgr.SendClientName(ConfigManager.Singleton.Configuration.PlayerName);
            baseTarget = new RenderImage("baseTarget", 800, 640, ImageBufferFormats.BufferUnknown);
            baseTarget.AlphaMaskFunction = CompareFunctions.GreaterThan;

            realScreenWidthTiles = (float)Gorgon.CurrentClippingViewport.Width / map.tileSpacing;
            realScreenHeightTiles = (float)Gorgon.CurrentClippingViewport.Height / map.tileSpacing;

            screenSize = new System.Drawing.Point(Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);

            //scaleX = (float)Gorgon.CurrentClippingViewport.Width / (realScreenWidthTiles * map.tileSpacing);
            //scaleY = (float)Gorgon.CurrentClippingViewport.Height / (realScreenHeightTiles * map.tileSpacing);

            gameInterface = new GameInterfaceManager(map, atomManager, this);

            return true;
        }

        public override void Shutdown()
        {
            if (baseTarget != null)
            {
                //baseTarget.Dispose();
            }
            gameInterface.Shutdown();
            atomManager.Shutdown();
            map.Shutdown();
            atomManager = null; 
            map = null;
            prg.mNetworkMgr.Disconnect();
            prg.mNetworkMgr.MessageArrived -= new NetworkMsgHandler(mNetworkMgr_MessageArrived);
        }

        public override void Update( FrameEventArgs e )
        {
            lastUpdate = now;
            now = DateTime.Now;
            atomManager.Update();
            gameInterface.Update();
        }

        private void mNetworkMgr_MessageArrived(NetworkManager netMgr, NetIncomingMessage msg)
        {
            if (msg == null)
            {
                return;
            }
            switch (msg.MessageType)
            {
                case NetIncomingMessageType.Data:
                    NetMessage messageType = (NetMessage)msg.ReadByte();
                    switch (messageType)
                    {
                        case NetMessage.MapMessage:
                            map.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.AtomManagerMessage:
                            atomManager.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.PlayerSessionMessage:
                            playerController.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.SendMap:
                            RecieveMap(msg);
                            break;
                        //case NetMessage.ChatMessage:
                            //HandleChatMessage(msg);
                            //break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        public void RecieveMap(NetIncomingMessage msg)
        {
            int mapWidth = msg.ReadInt32();
            int mapHeight = msg.ReadInt32();

            TileType[,] tileArray = new TileType[mapWidth, mapHeight];
            TileState[,] tileStates = new TileState[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    tileArray[x, y] = (TileType)msg.ReadByte();
                    tileStates[x, y] = (TileState)msg.ReadByte();
                }
            }
            map.LoadNetworkedMap(tileArray, tileStates, mapWidth, mapHeight);
        }

        #endregion


        /* What are we doing here exactly? Well:
         * First we get the tile we are stood on, and try and make this the centre of the view. However if we're too close to one edge
         * we allow us to be drawn nearer that edge, and not in the middle of the screen.
         * We then find how far "into" the map we are (xTopLeft, yTopLeft), the position of the top left of the screen in WORLD
         * co-ordinates so we can work out what we need to draw, and what we dont need to (what's off screen).
         * Then we see if we've moved a tile recently or a flag has been set on the map that we need to update the visibility (a door 
         * opened for example).
         * We then loop through all the tiles, and draw the floor and the sides of the walls, as they will always be under us
         * and the atoms. Next we find all the atoms in view and draw them. Lastly we draw the top section of walls as they will
         * always be on top of us and atoms.
         * */
        public override void GorgonRender(FrameEventArgs e)
        {
            Gorgon.Screen.Clear(System.Drawing.Color.Black);

            if (playerController.controlledAtom != null)
            {
                System.Drawing.Point centerTile = map.GetTileArrayPositionFromWorldPosition(playerController.controlledAtom.position);
              
                int xStart = System.Math.Max(0, centerTile.X - (screenWidthTiles / 2) - 1);
                int yStart = System.Math.Max(0, centerTile.Y - (screenHeightTiles / 2) - 1);
                int xEnd = System.Math.Min(xStart + screenWidthTiles + 2, map.mapWidth);
                int yEnd = System.Math.Min(yStart + screenHeightTiles + 2, map.mapHeight);

                xTopLeft = Math.Max(0, playerController.controlledAtom.position.X - ((screenWidthTiles / 2) * map.tileSpacing));
                yTopLeft = Math.Max(0, playerController.controlledAtom.position.Y - ((screenHeightTiles / 2) * map.tileSpacing));

                if (centerTile != map.lastVisPoint || map.needVisUpdate)
                {
                    map.compute_visibility(centerTile.X, centerTile.Y);
                    map.lastVisPoint = centerTile;
                }

                if (map.tileArray != null)
                {
                    for (int x = xStart; x < xEnd; x++)
                    {
                        for (int y = yStart; y < yEnd; y++)
                        {
                            if (map.tileArray[x, y].tileType == TileType.Wall)
                            {
                                if (y <= centerTile.Y)
                                {
                                    map.tileArray[x, y].Render(xTopLeft, yTopLeft, map.tileSpacing, lightsLastFrame);
                                }
                            }
                            else
                            {
                                map.tileArray[x, y].Render(xTopLeft, yTopLeft, map.tileSpacing, lightsLastFrame);
                            }
                        }
                    }
                }

                lightsThisFrame.Clear();

                if (atomManager != null)
                {
                    IEnumerable<Atom.Atom> atoms = from a in atomManager.atomDictionary.Values
                                                   where
                                                   a.visible &&
                                                   System.Math.Sqrt((playerController.controlledAtom.position.X - a.position.X) * (playerController.controlledAtom.position.X - a.position.X)) < screenHeightTiles * map.tileSpacing + 160 &&
                                                   System.Math.Sqrt((playerController.controlledAtom.position.Y - a.position.Y) * (playerController.controlledAtom.position.Y - a.position.Y)) < screenHeightTiles * map.tileSpacing + 160
                                                   select a;

                    foreach (Atom.Atom a in atoms)
                    {
                        if (a.light != null) lightsThisFrame.Add(a.light);

                        if (a is SS3D.Atom.Mob.Mob)
                        {
                            SS3D.Atom.Mob.Mob mob = (SS3D.Atom.Mob.Mob)a;

                            if (mob.GetItemOnAppendage("LeftHand") != null)
                                if (mob.GetItemOnAppendage("LeftHand").light != null)
                                    lightsThisFrame.Add(mob.GetItemOnAppendage("LeftHand").light);

                            if (mob.GetItemOnAppendage("RightHand") != null)
                                if (mob.GetItemOnAppendage("RightHand").light != null)
                                    lightsThisFrame.Add(mob.GetItemOnAppendage("RightHand").light);
                        }

                        a.Render(xTopLeft, yTopLeft, lightsLastFrame);

                        if (gameInterface.isBuilding) //Needs to happen after rendering since rendering sets the sprite pos.
                        {
                            a.sprite.UpdateAABB();

                            if (a.sprite.AABB.IntersectsWith(gameInterface.buildingAABB))
                            {
                                gameInterface.buildingBlocked = true;
                            }
                        }

                        if (showDebug)
                        {
                            Gorgon.Screen.Circle(a.sprite.BoundingCircle.Center.X, a.sprite.BoundingCircle.Center.Y, a.sprite.BoundingCircle.Radius, System.Drawing.Color.Orange);
                            Gorgon.Screen.Rectangle(a.sprite.AABB.X, a.sprite.AABB.Y, a.sprite.AABB.Width, a.sprite.AABB.Height, System.Drawing.Color.Blue);
                        }

                    }
                }

                if (map.tileArray != null)
                {
                    for (int x = xStart; x < xEnd; x++)
                    {
                        for (int y = yStart; y < yEnd; y++)
                        {
                            if (map.tileArray[x, y].tileType == TileType.Wall)
                            {
                                map.tileArray[x, y].RenderTop(xTopLeft, yTopLeft, map.tileSpacing, lightsLastFrame);
                            }
                        }
                    }
                }
                lightsLastFrame = lightsThisFrame;

                gameInterface.Draw(); //This needs to happen last so buildingBlocked resets properly. Sorry about that.
            }
            return;
        }

        // Not currently used.
        public override void FormResize()
        {
            scaleX = (float)Gorgon.CurrentClippingViewport.Width / (realScreenWidthTiles * map.tileSpacing);
            scaleY = (float)Gorgon.CurrentClippingViewport.Height / (realScreenHeightTiles * map.tileSpacing);
            screenSize = new System.Drawing.Point(Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);
        }

        #region Input

        public override void KeyDown(KeyboardInputEventArgs e)
        {
            if (e.Key == KeyboardKeys.F1)
            {
                Gorgon.FrameStatsVisible = !Gorgon.FrameStatsVisible;
            }
            if (e.Key == KeyboardKeys.F2)
            {
                showDebug = !showDebug;
            }
            if (e.Key == KeyboardKeys.F3)
            {
                playerController.SendVerb("toxins", 0);
            }
            playerController.KeyDown(e.Key);
            if (e.Key == KeyboardKeys.F4)
            {
                gameInterface.StartBuilding(typeof(Atom.Item.Container.Toolbox));
            }
            playerController.KeyDown(e.Key);
        }
        public override void KeyUp(KeyboardInputEventArgs e)
        {
            playerController.KeyUp(e.Key); // We want to pass key up events regardless of UI focus.
        }
        public override void MouseUp(MouseInputEventArgs e)
        {
        
        }
        public override void MouseDown(MouseInputEventArgs e)
        {

            if (gameInterface.isBuilding)
            {
                gameInterface.PlaceBuilding();
                return;
            }

            #region Object clicking
            bool atomClicked = false;
            // Convert our click from screen -> world coordinates
            Vector2D worldPosition = new Vector2D(e.Position.X + xTopLeft, e.Position.Y + yTopLeft);
            // A bounding box for our click
            System.Drawing.RectangleF mouseAABB = new System.Drawing.RectangleF(worldPosition.X, worldPosition.Y, 1, 1);

            // Find all the atoms near us we could have clicked
            IEnumerable<Atom.Atom> atoms = from a in atomManager.atomDictionary.Values
                                           where
                                           System.Math.Sqrt((playerController.controlledAtom.position.X - a.position.X) * (playerController.controlledAtom.position.X - a.position.X)) < map.tileSpacing * 1.5f &&
                                           System.Math.Sqrt((playerController.controlledAtom.position.Y - a.position.Y) * (playerController.controlledAtom.position.Y - a.position.Y)) < map.tileSpacing * 1.5f &&
                                           a.visible
                                           select a;
            // See which one our click AABB intersected with
            foreach (Atom.Atom a in atoms)
            {
                if (a.WasClicked(mouseAABB.Location))
                {
                    a.HandleClick();
                    atomClicked = true; // We clicked an atom so we don't want to send a turf click message too.
                }

            }

            if (!atomClicked)
            {
                System.Drawing.Point clickedPoint = map.GetTileArrayPositionFromWorldPosition(worldPosition);
                if (clickedPoint.X > 0 && clickedPoint.Y > 0)
                {
                    NetOutgoingMessage message = mStateMgr.prg.mNetworkMgr.netClient.CreateMessage();
                    message.Write((byte)NetMessage.MapMessage);
                    message.Write((short)clickedPoint.X);
                    message.Write((short)clickedPoint.Y);
                    mStateMgr.prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
                }
            } 
            #endregion
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            mousePosScreen = new Vector2D(e.Position.X, e.Position.Y);
            mousePosWorld = new Vector2D(e.Position.X + xTopLeft, e.Position.Y + yTopLeft);
        }
 
        #endregion

    }

}

#region Old / Depreciated Methods


        /*private void SetUpGUI()
        {
            // The chatbox
            gameChat = new Chatbox("gameChat");
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(gameChat.chatGUI);
            gameChat.chatPanel.ResizeMode = Miyagi.UI.ResizeModes.None;
            gameChat.chatPanel.Movable = false;
            gameChat.Transparency = 80;
            gameChat.TextSubmitted += new Chatbox.TextSubmitHandler(chatTextbox_TextSubmitted);


            guiGameScreen = new GUI("guiGameScreen");
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(guiGameScreen);
            Point screenSize = new Point((int)mEngine.Window.Width, (int)mEngine.Window.Height);

            HealthPanel healthPanel = new HealthPanel(mEngine);
            healthPanel.Initialize();

            // The health background
            Panel healthPanel = new Panel("healthPanel")
            {
                Size = new Size(48, 105),
                Location = new Point(10, screenSize.Y - 115),
                Skin = MiyagiResources.Singleton.Skins["HealthPanelSkin"],
            };

            // The actual health graphic - this will need to be changed if we do regional damage.
            PictureBox healthBodyBox = new PictureBox("healthBodyBox")
            {
                Size = new Size(42, 99),
                Location = new Point(2, 3),
                Bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromFile("../../../Media/GUI/HuD/healthgreen.png")
            };


            Button leftHandButton = new Button("leftHandButton")
            {
                Size = new Size(70, 61),
                Location = new Point(68, screenSize.Y - 71),
                Skin = MiyagiResources.Singleton.Skins["LeftHandButtonSkin"],
                TabStop = false
            };
            leftHandButton.MouseDown += LeftHandButtonMouseDown;

            Button rightHandButton = new Button("rightHandButton")
            {
                Size = new Size(70, 61),
                Location = new Point(143, screenSize.Y - 71),
                Skin = MiyagiResources.Singleton.Skins["RightHandButtonSkin"],
                TabStop = false
            };
            rightHandButton.MouseDown += RightHandButtonMouseDown;

            // These two boxes contain the pictures of the item we are holding in that hand. They are set in the itemmanager
            // when we recieve a message that we successfully picked up an item, that is why their name doesn't follow the
            // convention.
            PictureBox leftHandBox = new PictureBox("LHandBox")
            {
                Size = new Size(28, 48),
                Location = new Point(15, 5)
            };

            PictureBox rightHandBox = new PictureBox("RHandBox")
            {
                Size = new Size(28, 48),
                Location = new Point(15, 5)
            };


            Panel fpsPanel = new Panel("FPSPanel")
            {
                Size = new Size(128, 64),
                Location = new Point(10, screenSize.Y - 200),
            };

            fpsLabel1 = new Label()
            {
                Size = new Size(128, 16),
                Location = new Point(0, 0),
                TextStyle =
                {
                    ForegroundColour = Colours.White
                }

            };
            fpsPanel.Controls.Add(fpsLabel1);
            fpsLabel2 = new Label()
            {
                Size = new Size(128, 16),
                Location = new Point(0, 16),
                TextStyle =
                {
                    ForegroundColour = Colours.White
                }

            };
            fpsPanel.Controls.Add(fpsLabel2);
            fpsLabel3 = new Label()
            {
                Size = new Size(128, 16),
                Location = new Point(0, 32),
                TextStyle =
                {
                    ForegroundColour = Colours.White
                }

            };
            fpsPanel.Controls.Add(fpsLabel3);
            fpsLabel4 = new Label()
            {
                Size = new Size(128, 16),
                Location = new Point(0, 48),
                TextStyle =
                {
                    ForegroundColour = Colours.White
                }

            };
            fpsPanel.Controls.Add(fpsLabel4);
            fpsPanel.Visible = false;

            leftHandButton.Controls.Add(leftHandBox);
            rightHandButton.Controls.Add(rightHandBox);
                        
            guiGameScreen.Controls.Add(healthPanel.control);
            guiGameScreen.Controls.Add(leftHandButton);
            guiGameScreen.Controls.Add(rightHandButton);
            guiGameScreen.Controls.Add(fpsPanel);
            
          
        }*/

        /*private void HandleChatMessage(NetIncomingMessage msg)
        {
            ChatChannel channel = (ChatChannel)msg.ReadByte();
            string text = msg.ReadString();

            string message = "(" + channel.ToString() + "):" + text;
            ushort atomID = msg.ReadUInt16();
            gameChat.AddLine(message);
            Atom.Atom a = atomManager.GetAtom(atomID);
            if (a != null)
            {
                /*if (a.speechBubble == null) a.speechBubble = new SpeechBubble(mEngine, a.Entity);
                a.speechBubble.Show(text, 4000 + ((double)text.Length * (double)30));
            }
        }*/

        /*private void SendChatMessage(string text)
        {
            NetOutgoingMessage message = mEngine.mNetworkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)ChatChannel.Default);
            message.Write(text);

            mEngine.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }*/

        /*void chatTextbox_TextSubmitted(Chatbox chatbox, string text)
        {
            if (text == "/dumpmap")
            {
                if(map != null && itemManager != null)
                    MapFileHandler.SaveMap("./Maps/mapdump.map", map, itemManager);
            }
            else
            {
                SendChatMessage(text);
            }
        }*/

        /*public override void KeyUp(MOIS.KeyEvent keyState)
        {
            playerController.KeyUp(keyState.key); // We want to pass key up events regardless of UI focus.
            if (gameChat.HasFocus())
            {
                return;
            }
            else if (keyState.key == MOIS.KeyCode.KC_T)
            {
                gameChat.SetInputFocus();
            }
            else if (keyState.key == MOIS.KeyCode.KC_F1) // Toggle stats panel
            {
                showStats = !showStats;
                //guiGameScreen.GetControl("FPSPanel").Visible = showStats;
            }

        }*/

        /* public override void KeyDown(MOIS.KeyEvent keyState)
        {
            if (gameChat.HasFocus())
            {
                if (keyState.key == MOIS.KeyCode.KC_TAB)
                    gameChat.SetInputFocus(false);
                return;
            }

            /*if (keyState.key == MOIS.KeyCode.KC_ESCAPE)
                mStateMgr.RequestStateChange(typeof(MainMenu));

            // Pass keydown events to the PlayerController

            
            if (keyState.key == MOIS.KeyCode.KC_1)
            {
                guiGameScreen.GetControl("leftHandButton").Focused = true;
                guiGameScreen.GetControl("rightHandButton").Focused = false;
                mobManager.myMob.selectedHand = MobHand.LHand;
            }
            else if (keyState.key == MOIS.KeyCode.KC_2)
            {
                guiGameScreen.GetControl("leftHandButton").Focused = false;
                guiGameScreen.GetControl("rightHandButton").Focused = true;
                mobManager.myMob.selectedHand = MobHand.RHand;
            }
            else if (keyState.key == MOIS.KeyCode.KC_SPACE)
            {
                if (mobManager.myMob.selectedHand == MobHand.LHand)
                {
                    guiGameScreen.GetControl("leftHandButton").Focused = false;
                    guiGameScreen.GetControl("rightHandButton").Focused = true;
                    mobManager.myMob.selectedHand = MobHand.RHand;
                }
                else
                {
                    guiGameScreen.GetControl("leftHandButton").Focused = true;
                    guiGameScreen.GetControl("rightHandButton").Focused = false;
                    mobManager.myMob.selectedHand = MobHand.LHand;
                }
            }

        }*/

        /*public override void MouseDown(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
        {
            if (button == MOIS.MouseButtonID.MB_Right)
            {
                TimeSpan clickDiff = DateTime.Now - lastRMBClick;
                lastRMBClick = DateTime.Now;
                if (clickDiff.TotalMilliseconds < 250)
                {
                    mEngine.Camera.ParentNode.ResetOrientation();
                }
            }

            if (button == MOIS.MouseButtonID.MB_Left)
            {
                Point mouseLoc = mEngine.mMiyagiSystem.InputManager.MouseLocation;
                Vector2 mousePos = new Vector2((float)mouseLoc.X, (float)mouseLoc.Y);
                
                //Changed this because it is simpler to use a helper class just for raycasting, and we don't need the worldpos.
                Atom.Atom atom = HelperClasses.AtomUtil.PickAtScreenPosition(mEngine, mousePos);

                if (atom != null)
                {

                    atom.HandleClick();
                    /*switch (atom.AtomType)
                    {
                        case AtomType.Item:
                            itemManager.ClickItem((Item)atom);
                            break;
                        case AtomType.Mob:
                            mobManager.ClickMob((Mob)atom);
                            break;
                    }
                    
                }
            }
        }*/

        /*public override void MouseMove(MOIS.MouseEvent mouseState)
        {
            // r-button camera yaw
            if (mouseState.state.ButtonDown(MOIS.MouseButtonID.MB_Right))
            {
                int degree;
                if (mouseState.state.X.rel > lastMouseX)
                {
                    degree = -mouseState.state.X.rel;
                }
                else
                {
                    degree = mouseState.state.X.rel;
                }
                mEngine.Camera.ParentNode.Yaw(Mogre.Math.DegreesToRadians(degree), Node.TransformSpace.TS_WORLD);
                // uncomment to allow pitch control using the mouse y axis
                mEngine.Camera.ParentNode.Pitch(Mogre.Math.DegreesToRadians(mouseState.state.Y.rel), Node.TransformSpace.TS_LOCAL);
                
                lastMouseX = mouseState.state.X.abs;
            }

            // mousewheel camera zoom
            if (mouseState.state.Z.rel != 0)
            {
                mEngine.CameraDistance += mouseState.state.Z.rel / 6; 
                // single mousewheel tick is 120 units, so 20 units per tick
                mEngine.Camera.Position = new Mogre.Vector3(0, mEngine.CameraDistance, -2 * mEngine.CameraDistance / 3);
                // Offset the camera position to deal with atom node offsets. 
                //TODO make this less hackish
                //mEngine.Camera.Position = mEngine.Camera.Position + playerController.controlledAtom.offset;
           }
        }*/

        /*private void LeftHandButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MouseButton == MouseButton.Left)
            {
                playerController.SendVerb("selectlefthand", 0);
                //mobManager.myMob.selectedHand = MobHand.LHand;
            }
            else if (e.MouseButton == MouseButton.Right)
            {
                //itemManager.DropItem(MobHand.LHand);
            }
        }*/

        /*private void RightHandButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MouseButton == MouseButton.Left)
            {
                playerController.SendVerb("selectrighthand", 0);
                //mobManager.myMob.selectedHand = MobHand.RHand;
            }
            else if (e.MouseButton == MouseButton.Right)
            {
                //itemManager.DropItem(MobHand.RHand);
            }
        }*/


#endregion