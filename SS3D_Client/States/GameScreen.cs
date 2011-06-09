using System;
using System.IO;

using Mogre;
using Lidgren.Network;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Items;
using SS3D.Modules.Mobs;
using SS3D.Modules.Network;
using SS3D.Modules.UI;
using SS3D.Atom;

using SS3D_shared;

using System.Collections.Generic;
using System.Reflection;

using Miyagi;
using Miyagi.UI;
using Miyagi.UI.Controls;
using Miyagi.Common;
using Miyagi.Common.Data;
using Miyagi.Common.Resources;
using Miyagi.Common.Events;
using Miyagi.TwoD;

namespace SS3D.States
{
    public class GameScreen : State
    {
        #region Variables
        public OgreManager mEngine;
        private StateManager mStateMgr;
        public Map map;
        //private ItemManager itemManager;
        //private MobManager mobManager;
        private AtomManager atomManager;
        private GUI guiGameScreen;
        private Chatbox gameChat;
        private ushort defaultChannel;
        public PlayerController playerController;
        public DateTime lastUpdate;
        public DateTime now;

        private bool showStats = false;     // show FPS etc. panel if true
        private Label fpsLabel1, fpsLabel2, fpsLabel3, fpsLabel4;
        
        #region Mouse/Camera stuff
        private DateTime lastRMBClick = DateTime.Now;
        private int lastMouseX = 0;
        private int lastMouseY = 0;
        #endregion

        #endregion

        public GameScreen()
        {
            mEngine = null;
        }

        #region Startup, Shutdown, Update
        public override bool Startup(StateManager _mgr)
        {
            mEngine = _mgr.Engine;
            mStateMgr = _mgr;

            lastUpdate = DateTime.Now;
            now = DateTime.Now;

            defaultChannel = 1;

            mEngine.mMiyagiSystem.GUIManager.DisposeAllGUIs();

            map = new Map(mEngine, true);

            //mobManager = new MobManager(mEngine, map, mEngine.mNetworkMgr);
            //itemManager = new ItemManager(mEngine, map, mEngine.mNetworkMgr, mobManager);
            atomManager = new AtomManager(this);
            playerController = new PlayerController(this, atomManager);
            SetUp();
            SetUpGUI();

            mEngine.mNetworkMgr.MessageArrived += new NetworkMsgHandler(mNetworkMgr_MessageArrived);

            gameChat = new Chatbox("gameChat");
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(gameChat.chatGUI);
            gameChat.chatPanel.ResizeMode = Miyagi.UI.ResizeModes.None;
            gameChat.chatPanel.Movable = false;
            gameChat.Transparency = 25;
            defaultChannel = 1; 

            gameChat.TextSubmitted += new Chatbox.TextSubmitHandler(chatTextbox_TextSubmitted);

            mEngine.mNetworkMgr.SetMap(map);
            mEngine.mNetworkMgr.RequestMap();

            //TODO This should go somewhere else, there should be explicit session setup and teardown at some point.
            mEngine.mNetworkMgr.SendClientName(PlayerVars.PlayerName);
                                
            return true;
        }

        private void SetUp()
        {
            mEngine.SceneMgr.ShadowTextureSelfShadow = true;
            mEngine.SceneMgr.SetShadowTextureCasterMaterial("shadow_caster");
            mEngine.SceneMgr.SetShadowTexturePixelFormat(PixelFormat.PF_FLOAT16_RGB);
            mEngine.SceneMgr.ShadowCasterRenderBackFaces = false;
            mEngine.SceneMgr.SetShadowTextureSize(512);
            mEngine.SceneMgr.ShadowTechnique = ShadowTechnique.SHADOWTYPE_TEXTURE_ADDITIVE_INTEGRATED;
            
            mEngine.SceneMgr.AmbientLight = ColourValue.White;

            mEngine.SceneMgr.SetSkyBox(true, "SkyBox", 900f, true);
        }

        private void SetUpGUI()
        {
            // The chatbox
            gameChat = new Chatbox("gameChat");
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(gameChat.chatGUI);
            gameChat.chatPanel.ResizeMode = Miyagi.UI.ResizeModes.None;
            gameChat.chatPanel.Movable = false;
            gameChat.Transparency = 50;
            gameChat.TextSubmitted += new Chatbox.TextSubmitHandler(chatTextbox_TextSubmitted);


            guiGameScreen = new GUI("guiGameScreen");
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(guiGameScreen);
            Point screenSize = new Point((int)mEngine.Window.Width, (int)mEngine.Window.Height);

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

            healthPanel.Controls.Add(healthBodyBox);
            
            guiGameScreen.Controls.Add(healthPanel);
            guiGameScreen.Controls.Add(leftHandButton);
            guiGameScreen.Controls.Add(rightHandButton);
            guiGameScreen.Controls.Add(fpsPanel);
            
          
        }

        public override void Shutdown()
        {
            mEngine.mMiyagiSystem.GUIManager.GUIs.Remove(guiGameScreen);
            atomManager.Shutdown();
            map.Shutdown();

            atomManager = null; 
            map = null;
            //itemManager.Shutdown();
            //itemManager = null;
            //mobManager.Shutdown();
            //mobManager = null;
            mEngine.mNetworkMgr.Disconnect();
        }

        public override void Update(long _frameTime)
        {
            /* There's no reason to spam DateTime.Now everywhere. */
            lastUpdate = now;
            now = DateTime.Now;

            mEngine.SceneMgr.SkyBoxNode.Rotate(Mogre.Vector3.UNIT_Y, 0.0001f);

            //itemManager.Update();
            //mobManager.Update();
            atomManager.Update();

            if (showStats)
            {
                fpsLabel1.Text = String.Format("FPS: {0:F1}", mEngine.mWindow.LastFPS);
                fpsLabel2.Text = String.Format("Time: {0:F1}", 1000.0f / mEngine.mWindow.LastFPS);
                fpsLabel3.Text = String.Format("Batch: {0:F0}", mEngine.mWindow.BatchCount);
                fpsLabel4.Text = String.Format("Tris: {0:F0}", mEngine.mWindow.TriangleCount);
            }
            
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
                        case NetMessage.ChangeTile:
                            ChangeTile(msg);
                            break;
                        /*case NetMessage.ItemMessage:
                            itemManager.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.MobMessage:
                            mobManager.HandleNetworkMessage(msg);
                            break;
                         * */
                        case NetMessage.AtomManagerMessage:
                            atomManager.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.PlayerSessionMessage:
                            playerController.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.SendMap:
                            RecieveMap(msg);
                            break;
                        case NetMessage.ChatMessage:
                            HandleChatMessage(msg);
                            break;
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

            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    tileArray[x, z] = (TileType)msg.ReadByte();
                }
            }
            map.LoadNetworkedMap(tileArray, mapWidth, mapHeight);
        }

        private void ChangeTile(NetIncomingMessage msg)
        {
            if (map == null)
            {
                return;
            }
            int x = msg.ReadInt32();
            int z = msg.ReadInt32();
            TileType newTile = (TileType)msg.ReadByte();
            map.ChangeTile(x, z, newTile);
        }

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            ushort channel = msg.ReadUInt16();
            string text = msg.ReadString();

            string message = "(" + channel.ToString() + "):" + text;
            ushort atomID = msg.ReadUInt16();
            gameChat.AddLine(message);
            Atom.Atom a = atomManager.GetAtom(atomID);
            if(a != null)
                a.speaking = true;
        }

        private void SendChatMessage(string text)
        {
            NetOutgoingMessage message = mEngine.mNetworkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.ChatMessage);
            message.Write(defaultChannel);
            message.Write(text);

            mEngine.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        void chatTextbox_TextSubmitted(Chatbox chatbox, string text)
        {
            if (text == "/crowbar")
            {
                /*Mogre.Vector3 pos = mobManager.myMob.Node.Position;
                pos.y += 40;
                itemManager.SendCreateItem(pos);*/
            }
            else if (text == "/dumpmap")
            {
               /* if(map != null && itemManager != null)
                    MapFileHandler.SaveMap("./Maps/mapdump.map", map, itemManager);*/
            }
            else
            {
                SendChatMessage(text);
            }
        }

        #endregion

        #region Input
        public override void UpdateInput(Mogre.FrameEvent evt, MOIS.Keyboard keyState, MOIS.Mouse mouseState)
        {
            // TODO: Rewrite this shit
            if (gameChat.HasFocus())
            {
                return;
            }
            /*if(keyState.IsKeyDown(MOIS.KeyCode.KC_W))
            {
                mobManager.MoveMe(1);
                mobManager.Animate("walk");
            }
            if(keyState.IsKeyDown(MOIS.KeyCode.KC_D))
            {
                mobManager.MoveMe(2);
            }
            if (keyState.IsKeyDown(MOIS.KeyCode.KC_A))
            {
                mobManager.MoveMe(3);
            }
            if (keyState.IsKeyDown(MOIS.KeyCode.KC_S))
            {
                mobManager.MoveMe(4);
                mobManager.Animate("walk");
            }
            if (!keyState.IsKeyDown(MOIS.KeyCode.KC_W) && !keyState.IsKeyDown(MOIS.KeyCode.KC_S))
            {
                mobManager.Animate("idle");
            }*/
        }

        public override void KeyDown(MOIS.KeyEvent keyState)
        {
            if (gameChat.HasFocus())
            {
                return;
            }
            // Pass keydown events to the PlayerController
            playerController.KeyDown(keyState.key);

            /*if (keyState.key == MOIS.KeyCode.KC_LSHIFT)
            {
                mobManager.myMob.speed = mobManager.myMob.runSpeed;
            }

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
            else if (keyState.key == MOIS.KeyCode.KC_Q)
            {
                itemManager.DropItem(mobManager.myMob.selectedHand);
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
            }*/

        }

        public override void KeyUp(MOIS.KeyEvent keyState)
        {
            playerController.KeyUp(keyState.key); // We want to pass key up events regardless of UI focus.
            if (gameChat.HasFocus())
            {
                return;
            }
            /*if (keyState.key == MOIS.KeyCode.KC_LSHIFT)
            {
                mobManager.myMob.speed = mobManager.myMob.walkSpeed;
            }*/
            else if (keyState.key == MOIS.KeyCode.KC_T)
            {
                gameChat.SetInputFocus();
            }
            else if (keyState.key == MOIS.KeyCode.KC_F1) // Toggle stats panel
            {
                showStats = !showStats;
                guiGameScreen.GetControl("FPSPanel").Visible = showStats;
            }

        }

        public override void MouseUp(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
        {
        }

        public override void MouseDown(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
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
                    }*/
                    
                }
            }
        }

        public override void MouseMove(MOIS.MouseEvent mouseState)
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
                mEngine.Camera.Position = mEngine.Camera.Position + playerController.controlledAtom.offset;
           }
        }

        private void LeftHandButtonMouseDown(object sender, MouseButtonEventArgs e)
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
        }

        private void RightHandButtonMouseDown(object sender, MouseButtonEventArgs e)
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
        }
        #endregion

    }

}
