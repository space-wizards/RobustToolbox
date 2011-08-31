using System;
using System.IO;
using System.Linq;

using Lidgren.Network;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Network;
using SS3D.Modules.UI;
using SS3D.Modules.UI.Components;
using SS3D.Atom;
using SS3D.Effects;

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
        private GamePlacementManager gamePlacementMgr;

        //UI Vars
        #region UI Variables
        private Chatbox gameChat;
        public Dictionary<GuiComponentType, IGuiComponent> guiComponents;
        #endregion 

        private ushort defaultChannel;
        public PlayerController playerController;
        public DateTime lastUpdate;
        public DateTime now;
        private RenderImage baseTarget;
        private RenderImage lightTarget;
        private RenderImage lightTargetIntermediate;
        private Sprite baseTargetSprite;
        private Sprite lightTargetSprite;
        private Sprite lightTargetIntermediateSprite;
        private Batch gasBatch;
        private Batch wallTopsBatch;
        private Batch decalBatch;
        private Batch lightMapBatch;
        private GaussianBlur gaussianBlur;
        public bool blendLightMap = true;
        
        private List<Light> lightsLastFrame = new List<Light>();
        private List<Light> lightsThisFrame = new List<Light>();

        public int screenWidthTiles = 15; // How many tiles around us do we draw?
        public int screenHeightTiles = 12;

        private float realScreenWidthTiles = 0;
        private float realScreenHeightTiles = 0;

        private bool showStats = false;     // show FPS etc. panel if true
        private bool showDebug = false;     // show AABBs & Bounding Circles on atoms.
        private bool telepathy = false;     // disable visiblity bounds if true
        //private Label fpsLabel1, fpsLabel2, fpsLabel3, fpsLabel4;

        public float xTopLeft { get; private set; }
        public float yTopLeft { get; private set; }

        private float scaleX = 1.0f;
        private float scaleY = 1.0f;

        private System.Drawing.Point screenSize;
        public string spawnType = "";
        private bool editMode = false;
   
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

            //Hide the menu!
            prg.GorgonForm.MainMenuStrip.Hide();

            //TODO This should go somewhere else, there should be explicit session setup and teardown at some point.
            prg.mNetworkMgr.SendClientName(ConfigManager.Singleton.Configuration.PlayerName);

            baseTarget = new RenderImage("baseTarget", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);
            
            baseTargetSprite = new Sprite("baseTargetSprite", baseTarget);
            baseTargetSprite.DepthWriteEnabled = false;

            lightTarget = new RenderImage("lightTarget", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);
            lightTargetSprite = new Sprite("lightTargetSprite", lightTarget);
            lightTargetSprite.DepthWriteEnabled = false;
            lightTargetIntermediate = new RenderImage("lightTargetIntermediate", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);
            lightTargetIntermediateSprite = new Sprite("lightTargetIntermediateSprite", lightTargetIntermediate);
            lightTargetIntermediateSprite.DepthWriteEnabled = false;

            gasBatch = new Batch("gasBatch", 1);
            wallTopsBatch = new Batch("wallTopsBatch", 1);
            decalBatch = new Batch("decalBatch", 1);
            lightMapBatch = new Batch("lightMapBatch", 1);

            gaussianBlur = new GaussianBlur();
            
            realScreenWidthTiles = (float)Gorgon.CurrentClippingViewport.Width / map.tileSpacing;
            realScreenHeightTiles = (float)Gorgon.CurrentClippingViewport.Height / map.tileSpacing;

            screenSize = new System.Drawing.Point(Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);

            //scaleX = (float)Gorgon.CurrentClippingViewport.Width / (realScreenWidthTiles * map.tileSpacing);
            //scaleY = (float)Gorgon.CurrentClippingViewport.Height / (realScreenHeightTiles * map.tileSpacing);

            gamePlacementMgr = new GamePlacementManager(map, atomManager, this);

            //Init GUI components
            gameChat = new Chatbox("gameChat");
            gameChat.TextSubmitted += new Chatbox.TextSubmitHandler(chatTextbox_TextSubmitted);

            guiComponents = new Dictionary<GuiComponentType, IGuiComponent>();
            guiComponents.Add(GuiComponentType.AppendagesComponent, new HumanHandsGui(playerController));
            guiComponents.Add(GuiComponentType.StatPanelComponent, new StatPanelComponent(playerController));
            guiComponents[GuiComponentType.AppendagesComponent].Position = new System.Drawing.Point(Gorgon.Screen.Width - 190, Gorgon.Screen.Height - 99);
            

            return true;
        }

        public override void Shutdown()
        {
            if (baseTarget != null && Gorgon.IsInitialized)
            {
                baseTarget.ForceRelease();
                baseTarget.Dispose();
                
            }
            if (baseTargetSprite != null && Gorgon.IsInitialized)
            {
                baseTargetSprite.Name = null;
                baseTargetSprite.Image = null;
                baseTargetSprite = null;
            }
            gamePlacementMgr.Shutdown();
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
            gamePlacementMgr.Update();
            editMode = prg.GorgonForm.editMode;
            gamePlacementMgr.editMode = editMode;
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
                        case NetMessage.AtmosDisplayUpdate:
                            map.HandleAtmosDisplayUpdate(msg);
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

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            ChatChannel channel = (ChatChannel)msg.ReadByte();
            string text = msg.ReadString();

            string message = "(" + channel.ToString() + "):" + text;
            ushort atomID = msg.ReadUInt16();
            gameChat.AddLine(message, channel);
            Atom.Atom a = atomManager.GetAtom(atomID);
            if (a != null)
            {
                //if (a.speechBubble == null) a.speechBubble = new SpeechBubble(mEngine, a.Entity);
                //a.speechBubble.Show(text, 4000 + ((double)text.Length * (double)30));
            }
        }

        void chatTextbox_TextSubmitted(Chatbox chatbox, string text)
        {
            SendChatMessage(text);
        }

        private void SendChatMessage(string text)
        {
            NetOutgoingMessage message = prg.mNetworkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)ChatChannel.Player);
            message.Write(text);

            prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

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
            Gorgon.CurrentRenderTarget = baseTarget;

            baseTarget.Clear(System.Drawing.Color.Black);
            lightTarget.Clear(System.Drawing.Color.Black);
            lightTargetIntermediate.Clear(System.Drawing.Color.FromArgb(0,System.Drawing.Color.Black));
            Gorgon.Screen.Clear(System.Drawing.Color.Black);
            
            Gorgon.Screen.DefaultView.Left = 400;
            Gorgon.Screen.DefaultView.Top = 400;
            if (playerController.controlledAtom != null)
            {
                System.Drawing.Point centerTile = map.GetTileArrayPositionFromWorldPosition(playerController.controlledAtom.position);
              
                int xStart = System.Math.Max(0, centerTile.X - (screenWidthTiles / 2) - 1);
                int yStart = System.Math.Max(0, centerTile.Y - (screenHeightTiles / 2) - 1);
                int xEnd = System.Math.Min(xStart + screenWidthTiles + 2, map.mapWidth - 1);
                int yEnd = System.Math.Min(yStart + screenHeightTiles + 2, map.mapHeight - 1);

                xTopLeft = Math.Max(0, playerController.controlledAtom.position.X - ((screenWidthTiles / 2) * map.tileSpacing));
                yTopLeft = Math.Max(0, playerController.controlledAtom.position.Y - ((screenHeightTiles / 2) * map.tileSpacing));

                ///COMPUTE TILE VISIBILITY
                if (!telepathy && (centerTile != map.lastVisPoint || map.needVisUpdate))
                {
                    map.compute_visibility(centerTile.X, centerTile.Y);
                    map.lastVisPoint = centerTile;
                }
                else
                {
                    map.set_all_visible();
                }


                Tiles.Tile t;

                ///RENDER TILE BASES, PUT GAS SPRITES AND WALL TOP SPRITES INTO BATCHES TO RENDER LATER

                for (int x = xStart; x <= xEnd; x++)
                {
                    for (int y = yStart; y <= yEnd; y++)
                    {
                        t = map.tileArray[x, y];
                        if (!t.Visible)
                            continue;
                        if (t.tileType == TileType.Wall)
                        {
                            if (t.tilePosition.Y <= centerTile.Y)
                            {
                                t.Render(xTopLeft, yTopLeft, map.tileSpacing);
                                t.DrawDecals(xTopLeft, yTopLeft, map.tileSpacing, decalBatch);
                                t.RenderLight(xTopLeft, yTopLeft, map.tileSpacing, lightMapBatch);
                            }
                        }
                        else
                        {
                            t.Render(xTopLeft, yTopLeft, map.tileSpacing);
                            t.DrawDecals(xTopLeft, yTopLeft, map.tileSpacing, decalBatch);
                            t.RenderLight(xTopLeft, yTopLeft, map.tileSpacing, lightMapBatch);
                        }

                        ///Render gas sprites to gas batch
                        t.RenderGas(xTopLeft, yTopLeft, map.tileSpacing, gasBatch);
                        ///Render wall top sprites to wall top batch
                        t.RenderTop(xTopLeft, yTopLeft, map.tileSpacing, wallTopsBatch);
                    }
                }

                Gorgon.CurrentRenderTarget = lightTarget;
                if(lightMapBatch.Count > 0)
                    lightMapBatch.Draw();
                lightMapBatch.Clear();
                Gorgon.CurrentRenderTarget = baseTarget;

                ///Render decal batch
                if (decalBatch.Count > 0)
                    decalBatch.Draw();
                decalBatch.Clear();

                lightsThisFrame.Clear();
                
                ///RENDER ATOMS
                if (atomManager != null)
                {
                    IEnumerable<Atom.Atom> atoms = from a in atomManager.atomDictionary.Values
                                                   where
                                                   a.visible &&
                                                   a.position.X / map.tileSpacing >= xStart &&
                                                   a.position.X / map.tileSpacing <= xEnd &&
                                                   a.position.Y / map.tileSpacing >= yStart &&
                                                   a.position.Y / map.tileSpacing <= yEnd
                                                   orderby a.position.Y + ((a.sprite.Height * a.sprite.UniformScale) / 2) ascending
                                                   orderby a.drawDepth ascending
                                                   select a;

                    foreach (Atom.Atom a in atoms.ToList())
                    {
                        a.Render(xTopLeft, yTopLeft);

                        if (gamePlacementMgr.isBuilding) //Needs to happen after rendering since rendering sets the sprite pos.
                        {
                            a.sprite.UpdateAABB();

                            if (a.sprite.AABB.IntersectsWith(gamePlacementMgr.buildingAABB))
                            {
                                gamePlacementMgr.buildingBlocked = true;
                            }
                        }

                        if (showDebug)
                        {
                            Gorgon.Screen.Circle(a.sprite.BoundingCircle.Center.X, a.sprite.BoundingCircle.Center.Y, a.sprite.BoundingCircle.Radius, System.Drawing.Color.Orange);
                            Gorgon.Screen.Rectangle(a.sprite.AABB.X, a.sprite.AABB.Y, a.sprite.AABB.Width, a.sprite.AABB.Height, System.Drawing.Color.Blue);
                        }

                    }
                }

                ///Render gas batch
                if (gasBatch.Count > 0)
                    gasBatch.Draw();
                gasBatch.Clear();

                ///Render wall tops batch
                if (wallTopsBatch.Count > 0)
                    wallTopsBatch.Draw();
                wallTopsBatch.Clear();
                
                
                ///RENDER GHOSTS
                ///Render person ghosts to have them appear behind walls. This should really be 
                ///better thought out I think, but for now this works...
                /*if (atomManager != null)
                {
                    IEnumerable<Atom.Atom> atoms = from a in atomManager.atomDictionary.Values
                                                   where
                                                   a.IsChildOfType(typeof(Atom.Mob.Mob)) &&
                                                   a.visible &&
                                                   System.Math.Sqrt((playerController.controlledAtom.position.X - a.position.X) * (playerController.controlledAtom.position.X - a.position.X)) < screenHeightTiles * map.tileSpacing + 160 &&
                                                   System.Math.Sqrt((playerController.controlledAtom.position.Y - a.position.Y) * (playerController.controlledAtom.position.Y - a.position.Y)) < screenHeightTiles * map.tileSpacing + 160
                                                   orderby a.position.Y + ((a.sprite.Height * a.sprite.UniformScale) / 2) ascending
                                                   select a;

                    foreach (Atom.Atom a in atoms.ToList())
                    {
                        a.Render(xTopLeft, yTopLeft, 70);
                    }
                }*/

                gamePlacementMgr.Draw();
            }

            lightTargetSprite.DestinationBlend = AlphaBlendOperation.Zero;
            lightTargetSprite.SourceBlend = AlphaBlendOperation.One;

            gaussianBlur.SetSize(256.0f);
            gaussianBlur.PerformGaussianBlur(lightTargetSprite, lightTarget);
            gaussianBlur.SetSize(512.0f);
            gaussianBlur.PerformGaussianBlur(lightTargetSprite, lightTarget);
            gaussianBlur.SetSize(1024.0f);
            gaussianBlur.PerformGaussianBlur(lightTargetSprite, lightTarget);
            

            baseTargetSprite.Draw();

            if (blendLightMap)
            {
                lightTargetSprite.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha; // Use the alpha of the light to do bright/darkness
                lightTargetSprite.SourceBlend = AlphaBlendOperation.DestinationColor;
            }
            else
            {
                lightTargetSprite.DestinationBlend = AlphaBlendOperation.Zero; // Use the alpha of the light to do bright/darkness
                lightTargetSprite.SourceBlend = AlphaBlendOperation.One;
            }
            lightTargetSprite.Draw();

            Gorgon.CurrentRenderTarget = null;
            //baseTargetSprite.Draw();
            //Draw UI
            foreach (IGuiComponent component in guiComponents.Values)
            {
                component.Render();
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
            if (gameChat.Active)
                return;

            foreach (var comp in guiComponents.Values)
            {
                if (comp.KeyDown(e))//MouseDown returns true if the click is handled by the ui component.
                    return;
            }

            if (e.Key == KeyboardKeys.F9)
            {
                if (prg.GorgonForm.MainMenuStrip.Visible)
                {
                    prg.GorgonForm.MainMenuStrip.Hide();
                    prg.GorgonForm.MainMenuStrip.Visible = false;
                }
                else
                {
                    prg.GorgonForm.MainMenuStrip.Show();
                    prg.GorgonForm.MainMenuStrip.Visible = true;
                }
                    
            }
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
                gamePlacementMgr.StartBuilding(typeof(Atom.Item.Container.Toolbox));
            }
            if (e.Key == KeyboardKeys.F5)
            {
                playerController.SendVerb("save", 0);
            }
            if (e.Key == KeyboardKeys.F6)
            {
                telepathy = !telepathy;
            }
            if (e.Key == KeyboardKeys.F7)
            {
                blendLightMap = !blendLightMap;
            }

            //if (e.Key == KeyboardKeys.Left)
            //{
            //    if (gamePlacementMgr.isBuilding)
            //        gamePlacementMgr.Rotate(-90);
            //}
            //else if (e.Key == KeyboardKeys.Right)
            //{
            //    if (gamePlacementMgr.isBuilding)
            //        gamePlacementMgr.Rotate(90);
            //}

            playerController.KeyDown(e.Key);
        }
        public override void KeyUp(KeyboardInputEventArgs e)
        {
            playerController.KeyUp(e.Key); // We want to pass key up events regardless of UI focus.
        }
        public override void MouseUp(MouseInputEventArgs e)
        {
            //Forward clicks to gui components
            foreach (var comp in guiComponents.Values)
            {
                if(comp.MouseUp(e))
                    return;
            }
        }
        public override void MouseDown(MouseInputEventArgs e)
        {
            if (gamePlacementMgr.isBuilding)
            {
                gamePlacementMgr.PlaceBuilding();
                return;
            }
            if (playerController.controlledAtom == null)
                return;

            //Forward clicks to gui components
            foreach (var comp in guiComponents.Values)
            {
                if (comp.MouseDown(e))//MouseDown returns true if the click is handled by the ui component.
                    return;
            }

            #region Object clicking
            bool atomClicked = false;
            // Convert our click from screen -> world coordinates
            //Vector2D worldPosition = new Vector2D(e.Position.X + xTopLeft, e.Position.Y + yTopLeft);
            // A bounding box for our click
            System.Drawing.RectangleF mouseAABB = new System.Drawing.RectangleF(mousePosWorld.X, mousePosWorld.Y, 1, 1);
            float checkDistance = map.tileSpacing * 1.5f;
            // Find all the atoms near us we could have clicked
            if (editMode)
            {
                checkDistance = 500;
            }
            IEnumerable<Atom.Atom> atoms = from a in atomManager.atomDictionary.Values
                                           where
                                           System.Math.Sqrt((playerController.controlledAtom.position.X - a.position.X) * (playerController.controlledAtom.position.X - a.position.X)) < checkDistance &&
                                           System.Math.Sqrt((playerController.controlledAtom.position.Y - a.position.Y) * (playerController.controlledAtom.position.Y - a.position.Y)) < checkDistance &&
                                           a.visible
                                           orderby a.position.Y + ((a.sprite.Height * a.sprite.UniformScale) / 2) ascending
                                           orderby a.drawDepth ascending
                                           select a;
            // See which one our click AABB intersected with
            foreach (Atom.Atom a in atoms)
            {
                if (a.WasClicked(mouseAABB.Location))
                {
                    if (!editMode)
                    {
                        a.HandleClick();
                    }
                    else
                    {
                        if (e.Buttons == GorgonLibrary.InputDevices.MouseButtons.Right && prg.GorgonForm.GetAtomSpawnType() == null && a != playerController.controlledAtom)
                        {
                            NetOutgoingMessage message = mStateMgr.prg.mNetworkMgr.netClient.CreateMessage();
                            message.Write((byte)NetMessage.AtomManagerMessage);
                            message.Write((byte)AtomManagerMessage.DeleteAtom);
                            message.Write(a.uid);
                            mStateMgr.prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
                        }
                    }
                    atomClicked = true; // We clicked an atom so we don't want to send a turf click message too.
                    break;
                }

            }

            if (!atomClicked)
            {
                System.Drawing.Point clickedPoint = map.GetTileArrayPositionFromWorldPosition(mousePosWorld);
                if (clickedPoint.X > 0 && clickedPoint.Y > 0)
                {
                    if (editMode)
                    {
                        if (prg.GorgonForm.GetTileSpawnType() != TileType.None && !gamePlacementMgr.isBuilding)
                        {
                            NetOutgoingMessage message = mStateMgr.prg.mNetworkMgr.netClient.CreateMessage();
                            message.Write((byte)NetMessage.MapMessage);
                            message.Write((byte)MapMessage.TurfUpdate);
                            message.Write((short)clickedPoint.X);
                            message.Write((short)clickedPoint.Y);
                            message.Write((byte)prg.GorgonForm.GetTileSpawnType());
                            mStateMgr.prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
                        }
                    }
                    else
                    {
                        NetOutgoingMessage message = mStateMgr.prg.mNetworkMgr.netClient.CreateMessage();
                        message.Write((byte)NetMessage.MapMessage);
                        message.Write((byte)MapMessage.TurfClick);
                        message.Write((short)clickedPoint.X);
                        message.Write((short)clickedPoint.Y);
                        mStateMgr.prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
                    }
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