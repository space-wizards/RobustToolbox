using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

using CGO;
using ClientServices;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using Lidgren.Network;

using SS13.Effects;
using SS13.Modules;
using SS13.Modules.Network;
using SS13.UserInterface;
using SS13_Shared;
using ClientServices.Lighting;
using ClientServices.Map;
using ClientServices.Configuration;
using ClientInterfaces;
using ClientWindow;

namespace SS13.States
{
    public class GameScreen : State
    {
        #region Variables
        private StateManager _stateMgr;
        public MapManager map;
        private EntityManager _entityManager;
        private UiManager _uiManager;

        //UI Vars
        #region UI Variables
        private Chatbox gameChat;
        #endregion 

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

        private bool showDebug = false;     // show AABBs & Bounding Circles on Entities.
        private bool telepathy = false;     // disable visiblity bounds if true

        //public float xTopLeft { get; private set; }
        //public float yTopLeft { get; private set; }

        private float scaleX = 1.0f;
        private float scaleY = 1.0f;

        private System.Drawing.Point screenSize;
        public string spawnType = "";
        private bool editMode = false;
   
        #region Mouse/Camera stuff
        private DateTime lastRMBClick = DateTime.Now;

        public Vector2D mousePosScreen = Vector2D.Zero;
        public Vector2D mousePosWorld = Vector2D.Zero;

        #endregion

        private Vector2D WindowOrigin
        {
            get { return ClientWindowData.Singleton.ScreenOrigin; }
        }

        #endregion

        public GameScreen()
        {
        }

        #region Startup, Shutdown, Update
        public override bool Startup(Program _prg)
        {
            Program = _prg;
            _stateMgr = Program.StateManager;
            _uiManager = ServiceManager.Singleton.GetService<UiManager>();

            lastUpdate = DateTime.Now;
            now = DateTime.Now;

            ServiceManager.Singleton.Register<MapManager>();
            map = ServiceManager.Singleton.GetService<MapManager>();

            _uiManager.DisposeAllComponents();

            _entityManager = new EntityManager(Program.NetworkManager.netClient);
            PlayerController.Initialize(this);
            playerController = PlayerController.Singleton;

            Program.NetworkManager.MessageArrived += new NetworkMsgHandler(mNetworkMgr_MessageArrived);
            //prg.mNetworkMgr.Disconnected += new NetworkStateHandler(mNetworkMgr_Disconnected);

            Program.NetworkManager.SetMap(map);
            Program.NetworkManager.RequestMap();

            //Hide the menu!
            Program.GorgonForm.MainMenuStrip.Hide();

            //TODO This should go somewhere else, there should be explicit session setup and teardown at some point.
            Program.NetworkManager.SendClientName(ServiceManager.Singleton.GetService<ConfigurationManager>().Configuration.PlayerName);

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

            PlacementManager.Singleton.Initialize(Program.NetworkManager);

            //Init GUI components
            gameChat = new Chatbox("gameChat");
            gameChat.TextSubmitted += new Chatbox.TextSubmitHandler(chatTextbox_TextSubmitted);
            _uiManager.Components.Add(gameChat);

            HumanComboGUI combo = new HumanComboGUI(playerController, Program.NetworkManager);
            combo.Update();
            combo.Position = new Point(Gorgon.Screen.Width - combo.ClientArea.Width - 3, Gorgon.Screen.Height - combo.ClientArea.Height - 3);
            _uiManager.Components.Add(combo);
            _uiManager.Components.Add(new StatPanelComponent(playerController, Program.NetworkManager));

            return true;
        }

        //void mNetworkMgr_Disconnected(NetworkManager netMgr)
        //{
        //    mStateMgr.RequestStateChange(typeof(ConnectMenu)); //Fix this. Only temporary solution.
        //}

        public override void Shutdown()
        {
            if (baseTarget != null && Gorgon.IsInitialized)
            {
                baseTarget.ForceRelease();
                baseTarget.Dispose();
            }
            if (baseTargetSprite != null && Gorgon.IsInitialized)
            {
                baseTargetSprite.Image = null;
                baseTargetSprite = null;
            }
            if (lightTarget != null && Gorgon.IsInitialized)
            {
                lightTarget.ForceRelease();
                lightTarget.Dispose();
            }
            if (lightTargetSprite != null && Gorgon.IsInitialized)
            {
                lightTargetSprite.Image = null;
                lightTargetSprite = null;
            }
            if (lightTargetIntermediate != null && Gorgon.IsInitialized)
            {
                lightTargetIntermediate.ForceRelease();
                lightTargetIntermediate.Dispose();
            }
            if (lightTargetIntermediateSprite != null && Gorgon.IsInitialized)
            {
                lightTargetIntermediateSprite.Image = null;
                lightTargetIntermediateSprite = null;
            }
            gaussianBlur.Dispose();
            _entityManager.Shutdown();
            map.Shutdown();
            _entityManager = null;
            map = null;
            _uiManager.DisposeAllComponents(); //HerpDerp. This is probably bad. Should not remove them ALL.
            Program.NetworkManager.MessageArrived -= new NetworkMsgHandler(mNetworkMgr_MessageArrived);
            RenderTargetCache.DestroyAll();
            GC.Collect();
        }

        public override void Update( FrameEventArgs e )
        {
            lastUpdate = now;
            now = DateTime.Now;

            CGO.ComponentManager.Singleton.Update(e.FrameDeltaTime);
            editMode = Program.GorgonForm.EditMode;
            PlacementManager.Singleton.Update(mousePosScreen, map);
        }

        private void mNetworkMgr_MessageArrived(NetworkManager netMgr, NetIncomingMessage msg)
        {
            if (msg == null)
            {
                return;
            }
            switch (msg.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    NetConnectionStatus statMsg = (NetConnectionStatus)msg.ReadByte();
                    if (statMsg == NetConnectionStatus.Disconnected)
                    {
                        string discMsg = msg.ReadString();
                        _uiManager.Components.Add(new DisconnectedScreenBlocker(_stateMgr, discMsg));
                    }
                    break;
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
                        case NetMessage.PlayerSessionMessage:
                            playerController.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.PlayerUiMessage:
                            _uiManager.HandleNetMessage(msg);
                            break;
                        case NetMessage.PlacementManagerMessage:
                            PlacementManager.Singleton.HandleNetMessage(msg);
                            break;
                        case NetMessage.SendMap:
                            RecieveMap(msg);
                            break;
                        case NetMessage.ChatMessage:
                            HandleChatMessage(msg);
                            break;
                        case NetMessage.EntityMessage:
                            _entityManager.HandleEntityNetworkMessage(msg);
                            break;
                        case NetMessage.EntityManagerMessage:
                            _entityManager.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.RequestAdminLogin:
                            HandleAdminMessage(messageType, msg);
                            break;
                        case NetMessage.RequestAdminPlayerlist:
                            HandleAdminMessage(messageType, msg);
                            break;
                        case NetMessage.RequestBanList:
                            HandleAdminMessage(messageType, msg);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        public void HandleAdminMessage(NetMessage adminMsgType, NetIncomingMessage messageBody)
        {
            switch (adminMsgType)
            {
                case NetMessage.RequestAdminLogin:
                    _uiManager.DisposeAllComponentsOfType(typeof(AdminPasswordDialog)); //Remove old ones.
                    _uiManager.Components.Add(new AdminPasswordDialog(new System.Drawing.Size(200, 75), Program.NetworkManager)); //Create a new one.
                    break;
                case NetMessage.RequestAdminPlayerlist:
                    _uiManager.DisposeAllComponentsOfType(typeof(AdminPlayerPanel));
                    _uiManager.Components.Add(new AdminPlayerPanel(new System.Drawing.Size(600, 200), Program.NetworkManager, messageBody));
                    break;
                case NetMessage.RequestBanList:
                    Banlist banList = new Banlist();
                    int entriesCount = messageBody.ReadInt32();
                    for (int i = 0; i < entriesCount; i++)
                    {
                        string ip = messageBody.ReadString();
                        string reason = messageBody.ReadString();
                        bool tempBan = messageBody.ReadBoolean();
                        uint minutesLeft = messageBody.ReadUInt32();
                        BanEntry entry = new BanEntry();
                        entry.reason = reason;
                        entry.tempBan = tempBan;
                        entry.expiresAt = DateTime.Now.AddMinutes(minutesLeft);
                        banList.List.Add(entry);
                    }
                    _uiManager.DisposeAllComponentsOfType(typeof(AdminUnbanPanel));
                    _uiManager.Components.Add(new AdminUnbanPanel(new System.Drawing.Size(620, 200), Program.NetworkManager, banList));
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
            int entityId = msg.ReadInt32();
            gameChat.AddLine(message, channel);
            Entity a = EntityManager.Singleton.GetEntity(entityId);
            if (a != null)
            {
                a.SendMessage(this, SS13_Shared.GO.ComponentMessageType.EntitySaidSomething, null, channel, text);
                /*if (a.speechBubble == null) a.speechBubble = new SpeechBubble(a.name + a.Uid.ToString());
                if(channel == ChatChannel.Ingame || channel == ChatChannel.Player || channel == ChatChannel.Radio)
                    a.speechBubble.SetText(text);*/ //TODO re-enable speechbubbles
            }
        }

        void chatTextbox_TextSubmitted(Chatbox chatbox, string text)
        {
            SendChatMessage(text);
        }

        private void SendChatMessage(string text)
        {
            NetOutgoingMessage message = Program.NetworkManager.netClient.CreateMessage();
            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)ChatChannel.Player);
            message.Write(text);

            Program.NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        /* What are we doing here exactly? Well:
         * First we get the tile we are stood on, and try and make this the centre of the view. However if we're too close to one edge
         * we allow us to be drawn nearer that edge, and not in the middle of the screen.
         * We then find how far "into" the map we are (xTopLeft, yTopLeft), the position of the top left of the screen in WORLD
         * co-ordinates so we can work out what we need to draw, and what we dont need to (what's off screen).
         * Then we see if we've moved a tile recently or a flag has been set on the map that we need to update the visibility (a door 
         * opened for example).
         * We then loop through all the tiles, and draw the floor and the sides of the walls, as they will always be under us
         * and the entities. Next we find all the entities in view and draw them. Lastly we draw the top section of walls as they will
         * always be on top of us and entities.
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
            if (playerController.ControlledEntity != null)
            {
                
                System.Drawing.Point centerTile = map.GetTileArrayPositionFromWorldPosition(playerController.ControlledEntity.Position);
              
                int xStart = System.Math.Max(0, centerTile.X - (screenWidthTiles / 2) - 1);
                int yStart = System.Math.Max(0, centerTile.Y - (screenHeightTiles / 2) - 1);
                int xEnd = System.Math.Min(xStart + screenWidthTiles + 2, map.mapWidth - 1);
                int yEnd = System.Math.Min(yStart + screenHeightTiles + 2, map.mapHeight - 1);

                ClientWindowData.Singleton.UpdateViewPort(playerController.ControlledEntity.Position);

                //xTopLeft = Math.Max(0, playerController.controlledEntity.position.X - ((screenWidthTiles / 2) * map.tileSpacing));
                //yTopLeft = Math.Max(0, playerController.controlledEntity.position.Y - ((screenHeightTiles / 2) * map.tileSpacing));
                ///COMPUTE TILE VISIBILITY
                if ((centerTile != map.lastVisPoint || map.needVisUpdate))
                {
                    if (!telepathy)
                    {
                        map.compute_visibility(centerTile.X, centerTile.Y);
                        map.lastVisPoint = centerTile;
                    }
                    else
                    {
                        map.set_all_visible();
                    }
                }


                ClientServices.Map.Tiles.Tile t;

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
                                t.Render(WindowOrigin.X, WindowOrigin.Y, map.tileSpacing);
                                t.DrawDecals(WindowOrigin.X, WindowOrigin.Y, map.tileSpacing, decalBatch);
                                t.RenderLight(WindowOrigin.X, WindowOrigin.Y, map.tileSpacing, lightMapBatch);
                            }
                        }
                        else
                        {
                            t.Render(WindowOrigin.X, WindowOrigin.Y, map.tileSpacing);
                            t.DrawDecals(WindowOrigin.X, WindowOrigin.Y, map.tileSpacing, decalBatch);
                            t.RenderLight(WindowOrigin.X, WindowOrigin.Y, map.tileSpacing, lightMapBatch);
                        }

                        ///Render gas sprites to gas batch
                        t.RenderGas(WindowOrigin.X, WindowOrigin.Y, map.tileSpacing, gasBatch);
                        ///Render wall top sprites to wall top batch
                        t.RenderTop(WindowOrigin.X, WindowOrigin.Y, map.tileSpacing, wallTopsBatch);
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

                //Render renderable components
                ComponentManager.Singleton.Render(0);

                ///Render gas batch
                if (gasBatch.Count > 0)
                    gasBatch.Draw();
                gasBatch.Clear();

                ///Render wall tops batch
                if (wallTopsBatch.Count > 0)
                    wallTopsBatch.Draw();
                wallTopsBatch.Clear();
                
                PlacementManager.Singleton.Render();
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
            if (_uiManager.KeyDown(e)) //KeyDown returns true if the click is handled by the ui component.
                return;

            if (e.Key == KeyboardKeys.F9)
            {
                if (Program.GorgonForm.MainMenuStrip.Visible)
                {
                    Program.GorgonForm.MainMenuStrip.Hide();
                    Program.GorgonForm.MainMenuStrip.Visible = false;
                }
                else
                {
                    Program.GorgonForm.MainMenuStrip.Show();
                    Program.GorgonForm.MainMenuStrip.Visible = true;
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
                Program.NetGrapher.Toggle();
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

            if (e.Key == KeyboardKeys.F8)
            {
                NetOutgoingMessage message = Program.NetworkManager.netClient.CreateMessage();
                message.Write((byte)NetMessage.ForceRestart);
                Program.NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
            }

            if (e.Key == KeyboardKeys.F10)
            {
               _uiManager.DisposeAllComponentsOfType(typeof(TileSpawnPanel)); //Remove old ones.
               _uiManager.Components.Add(new TileSpawnPanel(new System.Drawing.Size(350, 410))); //Create a new one.
            }

            if (e.Key == KeyboardKeys.F11)
            {
                _uiManager.DisposeAllComponentsOfType(typeof(EntitySpawnPanel)); //Remove old ones.
                _uiManager.Components.Add(new EntitySpawnPanel(new System.Drawing.Size(350, 410))); //Create a new one.
            }

            if (e.Key == KeyboardKeys.F12)
            {
                NetOutgoingMessage message = Program.NetworkManager.netClient.CreateMessage();
                message.Write((byte)NetMessage.RequestAdminPlayerlist);
                Program.NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
            }

            playerController.KeyDown(e.Key);
        }

        public override void KeyUp(KeyboardInputEventArgs e)
        {
            playerController.KeyUp(e.Key);
        }
        public override void MouseUp(MouseInputEventArgs e)
        {
            _uiManager.MouseUp(e);
        }
        public override void MouseDown(MouseInputEventArgs e)
        {
            if (playerController.ControlledEntity == null)
                return;

            if (_uiManager.MouseDown(e))
                // MouseDown returns true if the click is handled by the ui component.
                return;

            if (PlacementManager.Singleton.is_active && !PlacementManager.Singleton.eraser)
            {
                if (e.Buttons == GorgonLibrary.InputDevices.MouseButtons.Left)
                {
                    PlacementManager.Singleton.HandlePlacement();
                    return;
                }
                else if (e.Buttons == GorgonLibrary.InputDevices.MouseButtons.Right)
                {
                    PlacementManager.Singleton.Clear();
                    return;
                }
                //else if (e.Buttons == GorgonLibrary.InputDevices.MouseButtons.Middle)
                //{
                //    PlacementManager.Singleton.nextRot();
                //}
            }

            #region Object clicking
            // Convert our click from screen -> world coordinates
            //Vector2D worldPosition = new Vector2D(e.Position.X + xTopLeft, e.Position.Y + yTopLeft);
            // A bounding box for our click
            System.Drawing.RectangleF mouseAABB = new RectangleF(mousePosWorld.X, mousePosWorld.Y, 1, 1);
            float checkDistance = map.tileSpacing * 1.5f;
            // Find all the entities near us we could have clicked
            IEnumerable<Entity> entities = EntityManager.Singleton.GetEntitiesInRange(playerController.ControlledEntity.Position, checkDistance);
                
            // See which one our click AABB intersected with
            List<ClickData> clickedEntities = new List<ClickData>();
            int drawdepthofclicked = 0;
            PointF clickedWorldPoint = new PointF(mouseAABB.X, mouseAABB.Y);
            foreach (Entity a in entities)
            {
                ClickableComponent clickable = (ClickableComponent)a.GetComponent(SS13_Shared.GO.ComponentFamily.Click);
                if (clickable != null)
                {
                    if (clickable.CheckClick(clickedWorldPoint, out drawdepthofclicked))
                        clickedEntities.Add(new ClickData((Entity)a, drawdepthofclicked));
                }

            }

            if (clickedEntities.Any())
            {
                Entity entToClick = (from cd in clickedEntities
                                     orderby cd.drawdepth descending
                                     orderby cd.clicked.Position.Y descending
                                     select cd.clicked).Last();

                if (PlacementManager.Singleton.eraser && PlacementManager.Singleton.is_active)
                {
                    PlacementManager.Singleton.HandleDeletion(entToClick);
                    return;
                }

                if (e.Buttons == GorgonLibrary.InputDevices.MouseButtons.Left)
                {
                    ClickableComponent c = (ClickableComponent)entToClick.GetComponent(SS13_Shared.GO.ComponentFamily.Click);
                    c.DispatchClick(playerController.ControlledEntity.Uid);
                }
                else if (e.Buttons == GorgonLibrary.InputDevices.MouseButtons.Right)
                {
                    _uiManager.Components.Add( new SS13.UserInterface.ContextMenu(entToClick, mousePosScreen, true) );
                    return;
                }
            }
            else
            {
                System.Drawing.Point clickedPoint = map.GetTileArrayPositionFromWorldPosition(mousePosWorld);
                if (clickedPoint.X > 0 && clickedPoint.Y > 0)
                {
                    NetOutgoingMessage message = _stateMgr.Program.NetworkManager.netClient.CreateMessage();
                    message.Write((byte)NetMessage.MapMessage);
                    message.Write((byte)MapMessage.TurfClick);
                    message.Write((short)clickedPoint.X);
                    message.Write((short)clickedPoint.Y);
                    _stateMgr.Program.NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
                }
            } 
            #endregion
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            float distanceToPrev = (mousePosScreen - new Vector2D(e.Position.X, e.Position.Y)).Length;
            mousePosScreen = new Vector2D(e.Position.X, e.Position.Y);
            mousePosWorld = new Vector2D(e.Position.X + WindowOrigin.X, e.Position.Y + WindowOrigin.Y);
            _uiManager.MouseMove(e);
        }

        public override void MouseWheelMove(MouseInputEventArgs e)
        {
            _uiManager.MouseWheelMove(e);
        } 
        #endregion

        private struct ClickData
        {
            public Entity clicked;
            public int drawdepth;
            public ClickData(Entity _clicked, int _drawdepth)
            {
                clicked = _clicked;
                drawdepth = _drawdepth;
            }
        }
    }

}