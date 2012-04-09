using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

using CGO;

using ClientInterfaces.GOC;
using ClientInterfaces.Resource;
using ClientInterfaces.State;
using ClientServices.Helpers;
using ClientServices.UserInterface.Components;
using ClientServices.UserInterface.Inventory;

using ClientWindow;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using ClientServices.Lighting;
using SS3D.LightTest;

using SS13_Shared.GO;
using ClientInterfaces.Lighting;


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

        #region Lighting
        bool bPlayerVision = false;
        ILight playerVision;

        QuadRenderer quadRenderer;
        ShadowMapResolver shadowMapResolver;
        LightArea lightArea128;
        LightArea lightArea256;
        LightArea lightArea512;
        LightArea lightArea1024;
        RenderImage screenShadows;
        private FXShader lightBlendShader;
        private RenderImage _sceneTarget;

        
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

            MapManager.Init();

            NetworkManager.MessageArrived += NetworkManagerMessageArrived;

            NetworkManager.RequestMap();

            // TODO This should go somewhere else, there should be explicit session setup and teardown at some point.
            NetworkManager.SendClientName(ConfigurationManager.GetPlayerName());

            _baseTarget = new RenderImage("baseTarget", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);

            _baseTargetSprite = new Sprite("baseTargetSprite", _baseTarget) { DepthWriteEnabled = false };

            _sceneTarget = new RenderImage("sceneTarget", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);

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

            var hotbar = new Hotbar(ResourceManager);
            hotbar.Position = new Point(5, 650);
            UserInterfaceManager.AddComponent(hotbar);

            #region Lighting
            quadRenderer = new QuadRenderer();
            quadRenderer.LoadContent();
            shadowMapResolver = new ShadowMapResolver(quadRenderer, ShadowmapSize.Size512, ShadowmapSize.Size1024, ResourceManager);
            shadowMapResolver.LoadContent();
            lightArea128 = new LightArea(ShadowmapSize.Size128);
            lightArea256 = new LightArea(ShadowmapSize.Size256);
            lightArea512 = new LightArea(ShadowmapSize.Size512);
            lightArea1024 = new LightArea(ShadowmapSize.Size1024);
            screenShadows = new RenderImage("screenShadows", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);
            screenShadows.UseDepthBuffer = false;
            lightBlendShader = IoCManager.Resolve<IResourceManager>().GetShader("lightblend");

            playerVision = IoCManager.Resolve<ILightManager>().CreateLight();
            playerVision.SetColor(Color.Transparent);
            playerVision.SetRadius(1024);
            playerVision.Move(Vector2D.Zero);
            #endregion
        }

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

            ComponentManager.Singleton.Update(e.FrameDeltaTime);
            PlacementManager.Update(MousePosScreen, MapManager);

            MousePosWorld = new Vector2D(MousePosScreen.X + WindowOrigin.X, MousePosScreen.Y + WindowOrigin.Y);
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
            var entityId = msg.ReadInt32();
            string message;
            switch (channel)
            {
                case ChatChannel.Emote:
                    message = _entityManager.GetEntity(entityId).Name + " " + text;
                    break;
                case ChatChannel.Damage:
                    message = text;
                    break;
                default:
                    message = "[" + channel + "] " + text;
                    break;
            }
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

        public void GorgonRender(FrameEventArgs e)
        {
            Gorgon.CurrentRenderTarget = _baseTarget;

            _baseTarget.Clear(System.Drawing.Color.Black);
            Gorgon.Screen.Clear(System.Drawing.Color.Black);

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

                var lights = IoCManager.Resolve<ILightManager>().lightsInRadius(
                    PlayerManager.ControlledEntity.Position, 768f);

                Vector2D blitPos;
                screenShadows.Clear(Color.Black); // Clear shadow rendertarget

                foreach(ILight l in lights)
                {
                    LightArea area = GetLightArea(RadiusToShadowMapSize(l.Radius));
                    area.LightPosition = l.Position;//mousePosWorld; // Set the light position
                    area.BeginDrawingShadowCasters(); // Start drawing to the light rendertarget
                    DrawWallsRelativeToLight(xStart, xEnd, yStart, yEnd, area); // Draw all shadowcasting stuff here in black
                    area.EndDrawingShadowCasters(); // End drawing to the light rendertarget
                    shadowMapResolver.ResolveShadows(area.renderTarget.Image, area.renderTarget, area.LightPosition); // Calc shadows

                    Gorgon.CurrentRenderTarget = screenShadows; // Set to shadow rendertarget

                    //Draw the shadow to the shadows target.
                    blitPos = new Vector2D((area.LightPosition.X - area.LightAreaSize.X * 0.5f) - WindowOrigin.X,
                        (area.LightPosition.Y - area.LightAreaSize.Y * 0.5f) - WindowOrigin.Y); // Find light draw pos
                    area.renderTarget.SourceBlend = AlphaBlendOperation.One; //Additive blending
                    area.renderTarget.DestinationBlend = AlphaBlendOperation.One; //Additive blending
                    area.renderTarget.Blit(blitPos.X, blitPos.Y, area.renderTarget.Width,
                    area.renderTarget.Height, l.Color, BlitterSizeMode.Crop); // Draw the lights effects
                    area.renderTarget.SourceBlend = AlphaBlendOperation.SourceAlpha; //reset blend mode
                    area.renderTarget.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha; //reset blend mode
                }

                #region Vision testing stuff
                if (bPlayerVision)
                {
                    playerVision.Move(PlayerManager.ControlledEntity.Position);
                    LightArea area = GetLightArea(RadiusToShadowMapSize(playerVision.Radius));
                    area.LightPosition = playerVision.Position;//mousePosWorld; // Set the light position
                    area.BeginDrawingShadowCasters(); // Start drawing to the light rendertarget
                    DrawWallsRelativeToLight(xStart, xEnd, yStart, yEnd, area); // Draw all shadowcasting stuff here in black
                    area.EndDrawingShadowCasters(); // End drawing to the light rendertarget
                    shadowMapResolver.ResolveShadows(area.renderTarget.Image, area.renderTarget, area.LightPosition); // Calc shadows

                    Gorgon.CurrentRenderTarget = screenShadows; // Set to shadow rendertarget

                    //Draw the shadow to the shadows target.
                    blitPos = new Vector2D((area.LightPosition.X - area.LightAreaSize.X * 0.5f) - WindowOrigin.X,
                        (area.LightPosition.Y - area.LightAreaSize.Y * 0.5f) - WindowOrigin.Y); // Find light draw pos
                    area.renderTarget.SourceBlend = AlphaBlendOperation.DestinationColor; //Additive blending
                    area.renderTarget.DestinationBlend = AlphaBlendOperation.SourceAlpha; //Additive blending
                    area.renderTarget.Blit(blitPos.X, blitPos.Y, area.renderTarget.Width,
                    area.renderTarget.Height, playerVision.Color, BlitterSizeMode.Crop); // Draw the lights effects
                    area.renderTarget.SourceBlend = AlphaBlendOperation.SourceAlpha; //reset blend mode
                    area.renderTarget.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha; //reset blend mode
                }
                #endregion

                //Set rendertarget to draw the rest of the scene
                Gorgon.CurrentRenderTarget = _sceneTarget;
                Gorgon.CurrentRenderTarget.Clear(Color.Black);
                
                // Draw rest of scene
                DrawGround(xStart, xEnd, yStart, yEnd, centerTile);
                ComponentManager.Singleton.Render(0);

                // Render decal batch
                if (_decalBatch.Count > 0)
                    _decalBatch.Draw();
                _decalBatch.Clear();

                //I don't remember what the fuck this does but it is essential.
                Gorgon.CurrentRenderTarget.SourceBlend = AlphaBlendOperation.DestinationColor;
                Gorgon.CurrentRenderTarget.DestinationBlend = AlphaBlendOperation.SourceColor;

                DrawWalls(xStart, xEnd, yStart, yEnd, centerTile, false);
                if (_wallTopsBatch.Count > 0)
                    _wallTopsBatch.Draw();
                _wallTopsBatch.Clear();
                ComponentManager.Singleton.Render(0);

                //Render the scene and lights together to compose the lit scene
                Gorgon.CurrentRenderTarget = null;
                Gorgon.CurrentShader = lightBlendShader.Techniques["LightBlend"];
                lightBlendShader.Parameters["LightTexture"].SetValue(screenShadows.Image);
                lightBlendShader.Parameters["SceneTexture"].SetValue(_sceneTarget.Image); 
                lightBlendShader.Parameters["AmbientLight"].SetValue(new Vector4D(0f, 0f, 0f, 1));
                screenShadows.Image.Blit(0, 0, screenShadows.Width, screenShadows.Height, Color.White, BlitterSizeMode.Crop); // Blit the shadow image on top of the screen
                Gorgon.CurrentShader = null;

                //Render the placement manager shit
                PlacementManager.Render();
            }

            Gorgon.CurrentRenderTarget = null;
        }

        #region Lighting

        private ShadowmapSize RadiusToShadowMapSize(int Radius)
        {
             switch(Radius)
            {
                case 128:
                    return ShadowmapSize.Size128;
                case 256:
                    return ShadowmapSize.Size256;
                case 512:
                    return ShadowmapSize.Size512;
                case 1024:
                    return ShadowmapSize.Size1024;
                default:
                    return ShadowmapSize.Size1024;
             }
        }

        private LightArea GetLightArea(ShadowmapSize size)
        {
            switch (size)
            {
                case ShadowmapSize.Size128:
                    return lightArea128;
                case ShadowmapSize.Size256:
                    return lightArea256;
                case ShadowmapSize.Size512:
                    return lightArea512;
                case ShadowmapSize.Size1024:
                    return lightArea1024;
                default:
                    return lightArea1024;
            }
        }

        // Draws all walls in the area around the light relative to it, and in black (test code, not pretty)
        private void DrawWallsRelativeToLight(int xStart, int xEnd, int yStart, int yEnd, LightArea area)
        {
            System.Drawing.Point centerTile = MapManager.GetTileArrayPositionFromWorldPosition(area.LightPosition);

            int xS = System.Math.Max(0, centerTile.X - (ScreenWidthTiles / 2) - 4);
            int yS = System.Math.Max(0, centerTile.Y - (ScreenHeightTiles / 2) - 4);
            int xE = System.Math.Min(xStart + ScreenWidthTiles + 4, MapManager.GetMapWidth() - 1);
            int yE = System.Math.Min(yStart + ScreenHeightTiles + 4, MapManager.GetMapHeight() - 1);

            ClientServices.Map.Tiles.Tile t;
            for (int x = xS; x <= xE; x++)
            {
                for (int y = yS; y <= yE; y++)
                {

                    t = (Map.Tiles.Tile)MapManager.GetTileAt(x, y);
                    if (t.TileType == TileType.Wall)
                    {
                        Vector2D pos = area.ToRelativePosition(t.Position);
                        t.RenderPos(pos.X, pos.Y, MapManager.GetTileSpacing(), (int)area.LightAreaSize.X);
                    }
                }
            }
        }

        // Draws all walls normally (test code, not pretty)
        private void DrawWalls(int xStart, int xEnd, int yStart, int yEnd, Point centerTile, bool rel)
        {
            ClientServices.Map.Tiles.Tile t;
            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    t = (Map.Tiles.Tile)MapManager.GetTileAt(x, y);
                    if (t.TileType == TileType.Wall)
                    {
                        t.Render(WindowOrigin.X, WindowOrigin.Y, MapManager.GetTileSpacing());
                    }
                    t.RenderTop(WindowOrigin.X, WindowOrigin.Y, MapManager.GetTileSpacing(), _wallTopsBatch);
                }
            }
        }

        // Draws all ground normally (test code, not pretty)
        private void DrawGround(int xStart, int xEnd, int yStart, int yEnd, Point centerTile)
        {
            ClientServices.Map.Tiles.Tile t;
            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    t = (Map.Tiles.Tile)MapManager.GetTileAt(x, y);
                    if (t.TileType != TileType.Wall)
                    {
                        t.Render(WindowOrigin.X, WindowOrigin.Y, MapManager.GetTileSpacing());
                    }
                }
            }
        }

        #endregion

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
                var lights = IoCManager.Resolve<ILightManager>().lightsInRadius(
                    PlayerManager.ControlledEntity.Position, 768f);
                Random r = new Random();
                int i = r.Next(lights.Length - 1);
                lights[i].SetColor(r.Next(255), r.Next(255), r.Next(255), r.Next(255));
            }
            if (e.Key == KeyboardKeys.F7)
            {
                bPlayerVision = !bPlayerVision;
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
                    UserInterfaceManager.AddComponent(new PlayerActionsWindow(new Size(150, 150), ResourceManager, actComp)); //Create a new one.
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
                            if (UserInterfaceManager.currentTargetingAction != null && (UserInterfaceManager.currentTargetingAction.TargetType == PlayerActionTargetType.Any || UserInterfaceManager.currentTargetingAction.TargetType == PlayerActionTargetType.Other))
                                UserInterfaceManager.SelectTarget((Entity)entToClick);
                            else
                            {
                                var c = (ClickableComponent)entToClick.GetComponent(ComponentFamily.Click);
                                c.DispatchClick(PlayerManager.ControlledEntity.Uid);
                            }
                        }
                        break;

                    case MouseButtons.Right:
                        if (UserInterfaceManager.currentTargetingAction != null)
                            UserInterfaceManager.CancelTargeting();
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
                            if (UserInterfaceManager.currentTargetingAction != null && UserInterfaceManager.currentTargetingAction.TargetType == PlayerActionTargetType.Point)
                            {
                                UserInterfaceManager.SelectTarget(new PointF(MousePosWorld.X, MousePosWorld.Y));
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
                            if (UserInterfaceManager.currentTargetingAction != null)
                                UserInterfaceManager.CancelTargeting();
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