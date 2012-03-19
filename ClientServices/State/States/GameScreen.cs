using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using CGO;
using ClientInterfaces.GOC;
using ClientInterfaces.State;
using ClientServices.Helpers;
using ClientServices.UserInterface.Components;
using ClientServices.UserInterface.Inventory;
using ClientWindow;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;
using ClientServices.Lighting;
using SS13_Shared.GO;

namespace ClientServices.State.States
{
    public class GameScreen : State, IState
    {
        #region Variables
        private EntityManager _entityManager;

        //UI Vars
        #region UI Variables
        private Chatbox _gameChat;
        #endregion 

        public DateTime LastUpdate;
        public DateTime Now;
        private RenderImage _baseTarget;
        private RenderImage _lightTarget;
        private RenderImage _lightTargetIntermediate;
        private Sprite _baseTargetSprite;
        private Sprite _lightTargetSprite;
        private Sprite _lightTargetIntermediateSprite;
        private Batch _gasBatch;
        private Batch _wallTopsBatch;
        private Batch _decalBatch;
        private Batch _lightMapBatch;
        private GaussianBlur _gaussianBlur;
        public bool BlendLightMap = true;
        
        private List<Light> _lightsLastFrame = new List<Light>();
        private readonly List<Light> _lightsThisFrame = new List<Light>();

        public int ScreenWidthTiles = 15; // How many tiles around us do we draw?
        public int ScreenHeightTiles = 12;

        private float _realScreenWidthTiles;
        private float _realScreenHeightTiles;

        private bool _showDebug;     // show AABBs & Bounding Circles on Entities.
        private bool _telepathy;     // disable visiblity bounds if true

        private float _scaleX = 1.0f;
        private float _scaleY = 1.0f;

        private Point _screenSize;
        public string SpawnType;
        private bool _editMode;
   
        #region Mouse/Camera stuff
        private DateTime _lastRmbClick = DateTime.Now;

        public Vector2D MousePosScreen = Vector2D.Zero;
        public Vector2D MousePosWorld = Vector2D.Zero;

        #endregion

        private Vector2D WindowOrigin
        {
            get { return ClientWindowData.Singleton.ScreenOrigin; }
        }

        #endregion

        public GameScreen(IDictionary<Type, object> managers)
            : base(managers)
        {
            
        }

        #region Startup, Shutdown, Update
        public void Startup()
        {
            LastUpdate = DateTime.Now;
            Now = DateTime.Now;

            UserInterfaceManager.DisposeAllComponents();

            _entityManager = new EntityManager(NetworkManager);

            NetworkManager.MessageArrived += NetworkManagerMessageArrived;

            NetworkManager.RequestMap();

            // TODO This should go somewhere else, there should be explicit session setup and teardown at some point.
            NetworkManager.SendClientName(ConfigurationManager.GetPlayerName());

            _baseTarget = new RenderImage("baseTarget", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);

            _baseTargetSprite = new Sprite("baseTargetSprite", _baseTarget) { DepthWriteEnabled = false };

            _lightTarget = new RenderImage("lightTarget", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);
            _lightTargetSprite = new Sprite("lightTargetSprite", _lightTarget) { DepthWriteEnabled = false };
            _lightTargetIntermediate = new RenderImage("lightTargetIntermediate", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);
            _lightTargetIntermediateSprite = new Sprite("lightTargetIntermediateSprite", _lightTargetIntermediate) { DepthWriteEnabled = false };

            _gasBatch = new Batch("gasBatch", 1);
            _wallTopsBatch = new Batch("wallTopsBatch", 1);
            _decalBatch = new Batch("decalBatch", 1);
            _lightMapBatch = new Batch("lightMapBatch", 1);

            _gaussianBlur = new GaussianBlur(ResourceManager);

            _realScreenWidthTiles = (float)Gorgon.CurrentClippingViewport.Width / MapManager.GetTileSpacing();
            _realScreenHeightTiles = (float)Gorgon.CurrentClippingViewport.Height / MapManager.GetTileSpacing();

            _screenSize = new Point(Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);

            //Init GUI components
            _gameChat = new Chatbox(ResourceManager, UserInterfaceManager, KeyBindingManager);
            _gameChat.TextSubmitted += ChatTextboxTextSubmitted;
            UserInterfaceManager.AddComponent(_gameChat);

            var combo = new HumanComboGui(PlayerManager, NetworkManager, ResourceManager, UserInterfaceManager);
            combo.Update();
            combo.Position = new Point(Gorgon.Screen.Width - combo.ClientArea.Width - 3, Gorgon.Screen.Height - combo.ClientArea.Height - 3);
            UserInterfaceManager.AddComponent(combo);
            UserInterfaceManager.AddComponent(new StatPanelComponent(ConfigurationManager.GetPlayerName(), PlayerManager, NetworkManager, ResourceManager));

            var statusBar = new StatusEffectBar(ResourceManager, PlayerManager);
            statusBar.Position = new Point(Gorgon.Screen.Width - 200, 10);
            UserInterfaceManager.AddComponent(statusBar);
        }

        //void mNetworkMgr_Disconnected(NetworkManager netMgr)
        //{
        //    mStateMgr.RequestStateChange(typeof(ConnectMenu)); //Fix this. Only temporary solution.
        //}

        public void Shutdown()
        {
            if (_baseTarget != null && Gorgon.IsInitialized)
            {
                _baseTarget.ForceRelease();
                _baseTarget.Dispose();
            }
            if (_baseTargetSprite != null && Gorgon.IsInitialized)
            {
                _baseTargetSprite.Image = null;
                _baseTargetSprite = null;
            }
            if (_lightTarget != null && Gorgon.IsInitialized)
            {
                _lightTarget.ForceRelease();
                _lightTarget.Dispose();
            }
            if (_lightTargetSprite != null && Gorgon.IsInitialized)
            {
                _lightTargetSprite.Image = null;
                _lightTargetSprite = null;
            }
            if (_lightTargetIntermediate != null && Gorgon.IsInitialized)
            {
                _lightTargetIntermediate.ForceRelease();
                _lightTargetIntermediate.Dispose();
            }
            if (_lightTargetIntermediateSprite != null && Gorgon.IsInitialized)
            {
                _lightTargetIntermediateSprite.Image = null;
                _lightTargetIntermediateSprite = null;
            }
            _gaussianBlur.Dispose();
            _entityManager.Shutdown();
            MapManager.Shutdown();
            UserInterfaceManager.DisposeAllComponents(); //HerpDerp. This is probably bad. Should not remove them ALL.
            NetworkManager.MessageArrived -= NetworkManagerMessageArrived;
            RenderTargetCache.DestroyAll();
            GC.Collect();
        }

        public void Update( FrameEventArgs e )
        {
            LastUpdate = Now;
            Now = DateTime.Now;

            if (PlacementManager.IsActive && (UserInterfaceManager.DragInfo.IsActive || ((UserInterface.UserInterfaceManager)UserInterfaceManager).targetingAction != null))
                PlacementManager.Clear(); //Cludgy hack. I don't care. Just want to get this working. Fuck all those interfaces.

            ComponentManager.Singleton.Update(e.FrameDeltaTime);
            PlacementManager.Update(MousePosScreen, MapManager);
        }

        private void NetworkManagerMessageArrived(object sender, IncomingNetworkMessageArgs args)
        {
            var message = args.Message;
            if (message == null)
            {
                return;
            }
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    var statMsg = (NetConnectionStatus)message.ReadByte();
                    if (statMsg == NetConnectionStatus.Disconnected)
                    {
                        var disconnectMessage = message.ReadString();
                        UserInterfaceManager.AddComponent(new DisconnectedScreenBlocker(StateManager, UserInterfaceManager, ResourceManager, disconnectMessage));
                    }
                    break;
                case NetIncomingMessageType.Data:
                    var messageType = (NetMessage)message.ReadByte();
                    switch (messageType)
                    {
                        case NetMessage.MapMessage:
                            MapManager.HandleNetworkMessage(message);
                            break;
                        case NetMessage.AtmosDisplayUpdate:
                            MapManager.HandleAtmosDisplayUpdate(message);
                            break;
                        case NetMessage.PlayerSessionMessage:
                            PlayerManager.HandleNetworkMessage(message);
                            break;
                        case NetMessage.PlayerUiMessage:
                            UserInterfaceManager.HandleNetMessage(message);
                            break;
                        case NetMessage.PlacementManagerMessage:
                            PlacementManager.HandleNetMessage(message);
                            break;
                        case NetMessage.SendMap:
                            RecieveMap(message);
                            break;
                        case NetMessage.ChatMessage:
                            HandleChatMessage(message);
                            break;
                        case NetMessage.EntityMessage:
                            _entityManager.HandleEntityNetworkMessage(message);
                            break;
                        case NetMessage.EntityManagerMessage:
                            _entityManager.HandleNetworkMessage(message);
                            break;
                        case NetMessage.RequestAdminLogin:
                            HandleAdminMessage(messageType, message);
                            break;
                        case NetMessage.RequestAdminPlayerlist:
                            HandleAdminMessage(messageType, message);
                            break;
                        case NetMessage.RequestBanList:
                            HandleAdminMessage(messageType, message);
                            break;
                    }
                    break;
            }
        }

        public void HandleAdminMessage(NetMessage adminMsgType, NetIncomingMessage messageBody)
        {
            switch (adminMsgType)
            {
                case NetMessage.RequestAdminLogin:
                    UserInterfaceManager.DisposeAllComponents<AdminPasswordDialog>(); //Remove old ones.
                    UserInterfaceManager.AddComponent(new AdminPasswordDialog(new Size(200, 50), NetworkManager, ResourceManager)); //Create a new one.
                    break;
                case NetMessage.RequestAdminPlayerlist:
                    UserInterfaceManager.DisposeAllComponents<AdminPlayerPanel>();
                    UserInterfaceManager.AddComponent(new AdminPlayerPanel(new Size(600, 200), NetworkManager, ResourceManager, messageBody));
                    break;
                case NetMessage.RequestBanList:
                    var banList = new Banlist();
                    var entriesCount = messageBody.ReadInt32();
                    for (var i = 0; i < entriesCount; i++)
                    {
                        var ipAddress = messageBody.ReadString();
                        var reason = messageBody.ReadString();
                        var tempBan = messageBody.ReadBoolean();
                        var minutesLeft = messageBody.ReadUInt32();
                        var entry = new BanEntry
                                        {
                                            ip = ipAddress,
                                            reason = reason,
                                            tempBan = tempBan,
                                            expiresAt = DateTime.Now.AddMinutes(minutesLeft)
                                        };
                        banList.List.Add(entry);
                    }
                    UserInterfaceManager.DisposeAllComponents<AdminUnbanPanel>();
                    UserInterfaceManager.AddComponent(new AdminUnbanPanel(new Size(620, 200), banList, NetworkManager, ResourceManager));
                    break;
            }
        }

        public void RecieveMap(NetIncomingMessage msg)
        {
            int mapWidth = msg.ReadInt32();
            int mapHeight = msg.ReadInt32();

            var tileArray = new TileType[mapWidth, mapHeight];
            var tileStates = new TileState[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    tileArray[x, y] = (TileType)msg.ReadByte();
                    tileStates[x, y] = (TileState)msg.ReadByte();
                }
            }
            MapManager.LoadNetworkedMap(tileArray, tileStates, mapWidth, mapHeight);
        }

        #endregion

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            var channel = (ChatChannel)msg.ReadByte();
            var text = msg.ReadString();

            var message = "[" + channel + "] " + text;
            var entityId = msg.ReadInt32();
            _gameChat.AddLine(message, channel);
            var a = EntityManager.Singleton.GetEntity(entityId);
            if (a != null)
            {
                a.SendMessage(this, ComponentMessageType.EntitySaidSomething, channel, text);
            }
        }

        void ChatTextboxTextSubmitted(Chatbox chatbox, string text)
        {
            SendChatMessage(text);
        }

        private void SendChatMessage(string text)
        {
            var message = NetworkManager.CreateMessage();
            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)ChatChannel.Player);
            message.Write(text);
            NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
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
        public void GorgonRender(FrameEventArgs e)
        {
            Gorgon.CurrentRenderTarget = _baseTarget;

            _baseTarget.Clear(Color.Black);
            _lightTarget.Clear(Color.Black);
            _lightTargetIntermediate.Clear(Color.FromArgb(0, Color.Black));
            Gorgon.Screen.Clear(Color.Black);

            Gorgon.Screen.DefaultView.Left = 400;
            Gorgon.Screen.DefaultView.Top = 400;
            if (PlayerManager.ControlledEntity != null)
            {
                
                var centerTile = MapManager.GetTileArrayPositionFromWorldPosition(PlayerManager.ControlledEntity.Position);
              
                var xStart = Math.Max(0, centerTile.X - (ScreenWidthTiles / 2) - 1);
                var yStart = Math.Max(0, centerTile.Y - (ScreenHeightTiles / 2) - 1);
                var xEnd = Math.Min(xStart + ScreenWidthTiles + 2, MapManager.GetMapWidth() - 1);
                var yEnd = Math.Min(yStart + ScreenHeightTiles + 2, MapManager.GetMapHeight() - 1);

                ClientWindowData.Singleton.UpdateViewPort(PlayerManager.ControlledEntity.Position);

                //xTopLeft = Math.Max(0, playerController.controlledEntity.position.X - ((screenWidthTiles / 2) * map.tileSpacing));
                //yTopLeft = Math.Max(0, playerController.controlledEntity.position.Y - ((screenHeightTiles / 2) * map.tileSpacing));
                // COMPUTE TILE VISIBILITY
                if ((centerTile != MapManager.GetLastVisiblePoint() || MapManager.NeedVisibilityUpdate()))
                {
                    if (!_telepathy)
                    {
                        MapManager.ComputeVisibility(centerTile.X, centerTile.Y);
                        MapManager.SetLastVisiblePoint(centerTile);
                    }
                    else
                    {
                        MapManager.SetAllVisible();
                    }
                }

                // RENDER TILE BASES, PUT GAS SPRITES AND WALL TOP SPRITES INTO BATCHES TO RENDER LATER

                for (var x = xStart; x <= xEnd; x++)
                {
                    for (var y = yStart; y <= yEnd; y++)
                    {
                        var t = MapManager.GetTileAt(x, y); 
                        if (!t.Visible)
                            continue;
                        if (t.TileType == TileType.Wall)
                        {
                            if (t.TilePosition.Y <= centerTile.Y)
                            {
                                t.Render(WindowOrigin.X, WindowOrigin.Y, MapManager.GetTileSpacing());
                                t.DrawDecals(WindowOrigin.X, WindowOrigin.Y, MapManager.GetTileSpacing(), _decalBatch);
                                t.RenderLight(WindowOrigin.X, WindowOrigin.Y, MapManager.GetTileSpacing(), _lightMapBatch);
                            }
                        }
                        else
                        {
                            t.Render(WindowOrigin.X, WindowOrigin.Y, MapManager.GetTileSpacing());
                            t.DrawDecals(WindowOrigin.X, WindowOrigin.Y, MapManager.GetTileSpacing(), _decalBatch);
                            t.RenderLight(WindowOrigin.X, WindowOrigin.Y, MapManager.GetTileSpacing(), _lightMapBatch);
                        }

                        // Render gas sprites to gas batch
                        t.RenderGas(WindowOrigin.X, WindowOrigin.Y, MapManager.GetTileSpacing(), _gasBatch);
                        // Render wall top sprites to wall top batch
                        t.RenderTop(WindowOrigin.X, WindowOrigin.Y, MapManager.GetTileSpacing(), _wallTopsBatch);
                    }
                }

                Gorgon.CurrentRenderTarget = _lightTarget;
                if(_lightMapBatch.Count > 0)
                    _lightMapBatch.Draw();
                _lightMapBatch.Clear();
                Gorgon.CurrentRenderTarget = _baseTarget;

                // Render decal batch
                if (_decalBatch.Count > 0)
                    _decalBatch.Draw();
                _decalBatch.Clear();

                _lightsThisFrame.Clear();

                //Render renderable components
                ComponentManager.Singleton.Render(0);

                // Render gas batch
                if (_gasBatch.Count > 0)
                    _gasBatch.Draw();
                _gasBatch.Clear();

                // Render wall tops batch
                if (_wallTopsBatch.Count > 0)
                    _wallTopsBatch.Draw();
                _wallTopsBatch.Clear();
                
                PlacementManager.Render();
            }

            _lightTargetSprite.DestinationBlend = AlphaBlendOperation.Zero;
            _lightTargetSprite.SourceBlend = AlphaBlendOperation.One;

            _gaussianBlur.SetSize(256.0f);
            _gaussianBlur.PerformGaussianBlur(_lightTargetSprite, _lightTarget);
            _gaussianBlur.SetSize(512.0f);
            _gaussianBlur.PerformGaussianBlur(_lightTargetSprite, _lightTarget);
            _gaussianBlur.SetSize(1024.0f);
            _gaussianBlur.PerformGaussianBlur(_lightTargetSprite, _lightTarget);
            
            _baseTargetSprite.Draw();

            if (BlendLightMap)
            {
                _lightTargetSprite.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha; // Use the alpha of the light to do bright/darkness
                _lightTargetSprite.SourceBlend = AlphaBlendOperation.DestinationColor;
            }
            else
            {
                _lightTargetSprite.DestinationBlend = AlphaBlendOperation.Zero; // Use the alpha of the light to do bright/darkness
                _lightTargetSprite.SourceBlend = AlphaBlendOperation.One;
            }
            _lightTargetSprite.Draw();

            Gorgon.CurrentRenderTarget = null;
        }

        // Not currently used.
        public void FormResize()
        {
            _scaleX = Gorgon.CurrentClippingViewport.Width / (_realScreenWidthTiles * MapManager.GetTileSpacing());
            _scaleY = Gorgon.CurrentClippingViewport.Height / (_realScreenHeightTiles * MapManager.GetTileSpacing());
            _screenSize = new Point(Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);
            UserInterfaceManager.ResizeComponents();
        }

        #region Input

        public void KeyDown(KeyboardInputEventArgs e)
        {
            if (UserInterfaceManager.KeyDown(e)) //KeyDown returns true if the click is handled by the ui component.
                return;

            if (e.Key == KeyboardKeys.F1)
            {
                Gorgon.FrameStatsVisible = !Gorgon.FrameStatsVisible;
            }
            if (e.Key == KeyboardKeys.F2)
            {
                _showDebug = !_showDebug;
            }
            if (e.Key == KeyboardKeys.F5)
            {
                PlayerManager.SendVerb("save", 0);
            }
            if (e.Key == KeyboardKeys.F6)
            {
                _telepathy = !_telepathy;
            }
            if (e.Key == KeyboardKeys.F7)
            {
                BlendLightMap = !BlendLightMap;
            }
            if (e.Key == KeyboardKeys.F8)
            {
                var message = NetworkManager.CreateMessage();
                message.Write((byte)NetMessage.ForceRestart);
                NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
            }
            if (e.Key == KeyboardKeys.F9)
            {
                UserInterfaceManager.DisposeAllComponents<PlayerActionsWindow>(); //Remove old ones.
                PlayerActionComp actComp = (PlayerActionComp)PlayerManager.ControlledEntity.GetComponent(ComponentFamily.PlayerActions);
                if(actComp != null)
                    UserInterfaceManager.AddComponent(new PlayerActionsWindow(new Size(150, 150), ResourceManager, (UserInterface.UserInterfaceManager)UserInterfaceManager, actComp)); //Create a new one.
            }
            if (e.Key == KeyboardKeys.F10)
            {
                UserInterfaceManager.DisposeAllComponents<TileSpawnPanel>(); //Remove old ones.
                UserInterfaceManager.AddComponent(new TileSpawnPanel(new Size(350, 410), ResourceManager, PlacementManager)); //Create a new one.
            }
            if (e.Key == KeyboardKeys.F11)
            {
                UserInterfaceManager.DisposeAllComponents<EntitySpawnPanel>(); //Remove old ones.
                UserInterfaceManager.AddComponent(new EntitySpawnPanel(new Size(350, 410), ResourceManager, PlacementManager)); //Create a new one.
            }
            if (e.Key == KeyboardKeys.F12)
            {
                var message = NetworkManager.CreateMessage();
                message.Write((byte)NetMessage.RequestAdminPlayerlist);
                NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
            }

            PlayerManager.KeyDown(e.Key);
        }

        public void KeyUp(KeyboardInputEventArgs e)
        {
            PlayerManager.KeyUp(e.Key);
        }
        public void MouseUp(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseUp(e);
        }
        public void MouseDown(MouseInputEventArgs e)
        {
            if (PlayerManager.ControlledEntity == null)
                return;

            if (UserInterfaceManager.MouseDown(e))
                // MouseDown returns true if the click is handled by the ui component.
                return;

            if (PlacementManager.IsActive && !PlacementManager.Eraser)
            {
                switch (e.Buttons)
                {
                    case MouseButtons.Left:
                        PlacementManager.HandlePlacement();
                        return;
                    case MouseButtons.Right:
                        PlacementManager.Clear();
                        return;
                }
            }

            #region Object clicking
            // Convert our click from screen -> world coordinates
            //Vector2D worldPosition = new Vector2D(e.Position.X + xTopLeft, e.Position.Y + yTopLeft);
            // A bounding box for our click
            var mouseAABB = new RectangleF(MousePosWorld.X, MousePosWorld.Y, 1, 1);
            var checkDistance = MapManager.GetTileSpacing() * 1.5f;
            // Find all the entities near us we could have clicked
            var entities = EntityManager.Singleton.GetEntitiesInRange(PlayerManager.ControlledEntity.Position, checkDistance);
                
            // See which one our click AABB intersected with
            var clickedEntities = new List<ClickData>();
            var clickedWorldPoint = new PointF(mouseAABB.X, mouseAABB.Y);
            foreach (var entity in entities)
            {
                var clickable = (ClickableComponent)entity.GetComponent(ComponentFamily.Click);
                if (clickable == null) continue;
                int drawdepthofclicked;
                if (clickable.CheckClick(clickedWorldPoint, out drawdepthofclicked))
                    clickedEntities.Add(new ClickData(entity, drawdepthofclicked));
            }

            UserInterface.UserInterfaceManager UiMgr = ((UserInterface.UserInterfaceManager)UserInterfaceManager);

            if (clickedEntities.Any())
            {
                var entToClick = (from cd in clickedEntities
                                     orderby cd.Drawdepth descending
                                     orderby cd.Clicked.Position.Y descending
                                     select cd.Clicked).Last();

                if (PlacementManager.Eraser && PlacementManager.IsActive)
                {
                    PlacementManager.HandleDeletion(entToClick);
                    return;
                }

                switch (e.Buttons)
                {
                    case MouseButtons.Left:
                        {
                            if (UiMgr.targetingAction != null && (UiMgr.targetingAction.targetType == PlayerActionTargetType.Any || UiMgr.targetingAction.targetType == PlayerActionTargetType.Other))
                                UiMgr.SelectTarget((Entity)entToClick);
                            else
                            {
                                var c = (ClickableComponent)entToClick.GetComponent(ComponentFamily.Click);
                                c.DispatchClick(PlayerManager.ControlledEntity.Uid);
                            }
                        }
                        break;

                    case MouseButtons.Right:
                        if (UiMgr.targetingAction != null)
                            UiMgr.CancelTargeting();
                        else
                            UserInterfaceManager.AddComponent(new ContextMenu(entToClick, MousePosScreen, ResourceManager, UserInterfaceManager));
                        break;
                }
            }
            else
            {
                switch (e.Buttons)
                {
                    case MouseButtons.Left:
                        {
                            if (UiMgr.targetingAction != null && UiMgr.targetingAction.targetType == PlayerActionTargetType.Point)
                            {
                                UiMgr.SelectTarget(new PointF(MousePosWorld.X, MousePosWorld.Y));
                            }
                            else
                            {
                                var clickedPoint = MapManager.GetTileArrayPositionFromWorldPosition(MousePosWorld);
                                if (clickedPoint.X > 0 && clickedPoint.Y > 0)
                                {
                                    NetOutgoingMessage message = NetworkManager.CreateMessage();
                                    message.Write((byte)NetMessage.MapMessage);
                                    message.Write((byte)MapMessage.TurfClick);
                                    message.Write((short)clickedPoint.X);
                                    message.Write((short)clickedPoint.Y);
                                    NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
                                }
                            }
                            break;
                        }
                    case MouseButtons.Right:
                        {
                            if (UiMgr.targetingAction != null)
                                UiMgr.CancelTargeting();
                            break;
                        }
            }
            } 
            #endregion
        }

        public void MouseMove(MouseInputEventArgs e)
        {
            var distanceToPrev = (MousePosScreen - new Vector2D(e.Position.X, e.Position.Y)).Length;
            MousePosScreen = new Vector2D(e.Position.X, e.Position.Y);
            MousePosWorld = new Vector2D(e.Position.X + WindowOrigin.X, e.Position.Y + WindowOrigin.Y);
            UserInterfaceManager.MouseMove(e);
        }

        public void MouseWheelMove(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseWheelMove(e);
        } 
        #endregion

        private struct ClickData
        {
            public readonly IEntity Clicked;
            public readonly int Drawdepth;

            public ClickData(IEntity clicked, int drawdepth)
            {
                Clicked = clicked;
                Drawdepth = drawdepth;
            }
        }
    }

}