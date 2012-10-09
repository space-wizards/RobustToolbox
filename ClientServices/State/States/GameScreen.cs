using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;

using CGO;

using ClientInterfaces.GOC;
using ClientInterfaces.Map;
using ClientInterfaces.Player;
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
using SS13_Shared.GO;
using ClientInterfaces.Lighting;
using SS3D.LightTest;
using ClientServices.Tiles;

namespace ClientServices.State.States
{
    public class GameScreen : State, IState
    {
        #region Variables
        private EntityManager _entityManager;

        //UI Vars
        #region UI Variables
        private Chatbox _gameChat;
        private HandsGui _handsGui;
        #endregion 

        #region Lighting
        bool bPlayerVision = true;
        ILight playerVision;

        QuadRenderer quadRenderer;
        ShadowMapResolver shadowMapResolver;
        LightArea lightArea128;
        LightArea lightArea256;
        LightArea lightArea512;
        LightArea lightArea1024;
        RenderImage screenShadows;
        RenderImage shadowIntermediate;
        private RenderImage shadowBlendIntermediate;
        private RenderImage playerOcclusionTarget;
        private FXShader lightBlendShader;
        private FXShader finalBlendShader;
        private FXShader lightMapShader;
        private RenderImage _sceneTarget;
        private RenderImage _tilesTarget;
        private RenderImage _overlayTarget;
        private RenderImage _composedSceneTarget;

        
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
        private Batch _floorBatch;
        private Batch _wallBatch;
        private GaussianBlur _gaussianBlur;
        public bool BlendLightMap = true;
        private bool _recalculateScene = true;
        private bool _redrawTiles = true;
        private bool _redrawOverlay = true;
        
        public int ScreenWidthTiles = 15; // How many tiles around us do we draw?
        public int ScreenHeightTiles = 12;

        private float _realScreenWidthTiles;
        private float _realScreenHeightTiles;

        private bool _showDebug;     // show AABBs & Bounding Circles on Entities.

        public string SpawnType;

        #region Mouse/Camera stuff

        public Vector2D MousePosScreen = Vector2D.Zero;
        public Vector2D MousePosWorld = Vector2D.Zero;

        #endregion

        /// <summary>
        /// Center point of the current render window.
        /// </summary>
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
            IoCManager.Resolve<IMapManager>().OnTileChanged += OnTileChanged;

            IoCManager.Resolve<IPlayerManager>().OnPlayerMove += OnPlayerMove;

            // TODO This should go somewhere else, there should be explicit session setup and teardown at some point.
            NetworkManager.SendClientName(ConfigurationManager.GetPlayerName());

            _baseTarget = new RenderImage("baseTarget", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);

            _baseTargetSprite = new Sprite("baseTargetSprite", _baseTarget) { DepthWriteEnabled = false };

            _sceneTarget = new RenderImage("sceneTarget", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);
            _tilesTarget = new RenderImage("tilesTarget", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);

            _overlayTarget = new RenderImage("overlayTarget", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);
            _overlayTarget.SourceBlend = AlphaBlendOperation.SourceAlpha;
            _overlayTarget.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha;
            _overlayTarget.SourceBlendAlpha = AlphaBlendOperation.SourceAlpha;
            _overlayTarget.DestinationBlendAlpha = AlphaBlendOperation.InverseSourceAlpha;

            _composedSceneTarget = new RenderImage("composedSceneTarget", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);

            _lightTarget = new RenderImage("lightTarget", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);
            _lightTargetSprite = new Sprite("lightTargetSprite", _lightTarget) { DepthWriteEnabled = false };
            _lightTargetIntermediate = new RenderImage("lightTargetIntermediate", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);
            _lightTargetIntermediateSprite = new Sprite("lightTargetIntermediateSprite", _lightTargetIntermediate) { DepthWriteEnabled = false };

            _gasBatch = new Batch("gasBatch", 1);
            _gasBatch.SourceBlend = AlphaBlendOperation.SourceAlpha;
            _gasBatch.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha;
            _gasBatch.SourceBlendAlpha = AlphaBlendOperation.SourceAlpha;
            _gasBatch.DestinationBlendAlpha = AlphaBlendOperation.InverseSourceAlpha;

            _wallTopsBatch = new Batch("wallTopsBatch", 1);
            _wallTopsBatch.SourceBlend = AlphaBlendOperation.SourceAlpha;
            _wallTopsBatch.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha;
            _wallTopsBatch.SourceBlendAlpha = AlphaBlendOperation.SourceAlpha;
            _wallTopsBatch.DestinationBlendAlpha = AlphaBlendOperation.InverseSourceAlpha;

            _decalBatch = new Batch("decalBatch", 1);
            _decalBatch.SourceBlend = AlphaBlendOperation.SourceAlpha;
            _decalBatch.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha;
            _decalBatch.SourceBlendAlpha = AlphaBlendOperation.SourceAlpha;
            _decalBatch.DestinationBlendAlpha = AlphaBlendOperation.InverseSourceAlpha;

            _floorBatch = new Batch("floorBatch", 1);
            _wallBatch = new Batch("wallBatch", 1);

            _gaussianBlur = new GaussianBlur(ResourceManager);

            _realScreenWidthTiles = (float)Gorgon.CurrentClippingViewport.Width / MapManager.GetTileSpacing();
            _realScreenHeightTiles = (float)Gorgon.CurrentClippingViewport.Height / MapManager.GetTileSpacing();
            
            //Init GUI components
            _gameChat = new Chatbox(ResourceManager, UserInterfaceManager, KeyBindingManager);
            _gameChat.TextSubmitted += ChatTextboxTextSubmitted;
            UserInterfaceManager.AddComponent(_gameChat);

            //UserInterfaceManager.AddComponent(new StatPanelComponent(ConfigurationManager.GetPlayerName(), PlayerManager, NetworkManager, ResourceManager));

            var statusBar = new StatusEffectBar(ResourceManager, PlayerManager);
            statusBar.Position = new Point(Gorgon.CurrentClippingViewport.Width - 800, 10);
            UserInterfaceManager.AddComponent(statusBar);

            var hotbar = new Hotbar(ResourceManager);
            hotbar.Position = new Point(5, Gorgon.CurrentClippingViewport.Height - hotbar.ClientArea.Height - 5);
            hotbar.Update(0);
            UserInterfaceManager.AddComponent(hotbar);

            #region Lighting
            quadRenderer = new QuadRenderer();
            quadRenderer.LoadContent();
            shadowMapResolver = new ShadowMapResolver(quadRenderer, ShadowmapSize.Size1024, ShadowmapSize.Size1024, ResourceManager);
            shadowMapResolver.LoadContent();
            lightArea128 = new LightArea(ShadowmapSize.Size128);
            lightArea256 = new LightArea(ShadowmapSize.Size256);
            lightArea512 = new LightArea(ShadowmapSize.Size512);
            lightArea1024 = new LightArea(ShadowmapSize.Size1024);
            screenShadows = new RenderImage("screenShadows", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);
            screenShadows.UseDepthBuffer = false;
            shadowIntermediate = new RenderImage("shadowIntermediate", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);
            shadowIntermediate.UseDepthBuffer = false;
            shadowBlendIntermediate = new RenderImage("shadowBlendIntermediate", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);
            shadowBlendIntermediate.UseDepthBuffer = false;
            playerOcclusionTarget = new RenderImage("playerOcclusionTarget", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);
            playerOcclusionTarget.UseDepthBuffer = false;
            lightBlendShader = IoCManager.Resolve<IResourceManager>().GetShader("lightblend");
            finalBlendShader = IoCManager.Resolve<IResourceManager>().GetShader("finallight");
            lightMapShader = IoCManager.Resolve<IResourceManager>().GetShader("lightmap");

            playerVision = IoCManager.Resolve<ILightManager>().CreateLight();
            playerVision.SetColor(Color.Transparent);
            playerVision.SetRadius(1024);
            playerVision.Move(Vector2D.Zero);
            #endregion

            _handsGui = new HandsGui();
            _handsGui.Position = new Point(hotbar.Position.X + 5, hotbar.Position.Y + 7);
            UserInterfaceManager.AddComponent(_handsGui);

            var combo = new HumanComboGui(PlayerManager, NetworkManager, ResourceManager, UserInterfaceManager);
            combo.Update(0);
            combo.Position = new Point(hotbar.ClientArea.Right - combo.ClientArea.Width + 5, hotbar.Position.Y - combo.ClientArea.Height - 5);
            UserInterfaceManager.AddComponent(combo);

            var healthPanel = new HealthPanel();
            healthPanel.Position = new Point(hotbar.ClientArea.Right - 1, hotbar.Position.Y + 11);
            healthPanel.Update(0);
            UserInterfaceManager.AddComponent(healthPanel);

            var targetingUi = new TargetingGui();
            targetingUi.Update(0);
            targetingUi.Position = new Point(healthPanel.ClientArea.Right - 1, healthPanel.ClientArea.Bottom - targetingUi.ClientArea.Height);
            UserInterfaceManager.AddComponent(targetingUi);

            var inventoryButton = new SimpleImageButton("button_inv", ResourceManager);
            inventoryButton.Position = new Point(hotbar.Position.X + 172, hotbar.Position.Y + 2);
            inventoryButton.Update(0);
            inventoryButton.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(inventoryButton_Clicked);
            UserInterfaceManager.AddComponent(inventoryButton);

            var statusButton = new SimpleImageButton("button_status", ResourceManager);
            statusButton.Position = new Point(inventoryButton.ClientArea.Right , inventoryButton.Position.Y);
            statusButton.Update(0);
            statusButton.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(statusButton_Clicked);
            UserInterfaceManager.AddComponent(statusButton);

            var craftButton = new SimpleImageButton("button_craft", ResourceManager);
            craftButton.Position = new Point(statusButton.ClientArea.Right , statusButton.Position.Y);
            craftButton.Update(0);
            craftButton.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(craftButton_Clicked);
            UserInterfaceManager.AddComponent(craftButton);

            var menuButton = new SimpleImageButton("button_menu", ResourceManager);
            menuButton.Position = new Point(craftButton.ClientArea.Right , craftButton.Position.Y);
            menuButton.Update(0);
            menuButton.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(menuButton_Clicked);
            UserInterfaceManager.AddComponent(menuButton);

        }

        void ResetRendertargets()
        {
            var w = Gorgon.CurrentClippingViewport.Width;
            var h = Gorgon.CurrentClippingViewport.Height;
            
            _baseTarget.Width = w;
            _baseTarget.Height = h;
            _sceneTarget.Width = w;
            _sceneTarget.Height = h;
            _tilesTarget.Width = w;
            _tilesTarget.Height = h;
            _overlayTarget.Width = w;
            _overlayTarget.Height = h;
            _composedSceneTarget.Width = w;
            _composedSceneTarget.Height = h;
            _lightTarget.Width = w;
            _lightTarget.Height = h;
            _lightTargetIntermediate.Width = w;
            _lightTargetIntermediate.Height = h;
            screenShadows.Width = w;
            screenShadows.Height = h;
            shadowIntermediate.Width = w;
            shadowIntermediate.Height = h;
            shadowBlendIntermediate.Width = w;
            shadowBlendIntermediate.Height = h;
            playerOcclusionTarget.Dispose();
            playerOcclusionTarget = new RenderImage("playerOcclusionTarget", Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, ImageBufferFormats.BufferRGB888A8);
            playerOcclusionTarget.Width = w;
            playerOcclusionTarget.Height = h;
            //playerOcclusionTarget.DeviceReset();
            _gaussianBlur.Dispose();
            _gaussianBlur = new GaussianBlur(ResourceManager);

        }

        void menuButton_Clicked(SimpleImageButton sender)
        {
            UserInterfaceManager.DisposeAllComponents<MenuWindow>(); //Remove old ones.
            UserInterfaceManager.AddComponent(new MenuWindow()); //Create a new one.
        }

        void craftButton_Clicked(SimpleImageButton sender)
        {
            UserInterfaceManager.ComponentUpdate(GuiComponentType.ComboGui, ComboGuiMessage.ToggleShowPage, 3);
        }

        void statusButton_Clicked(SimpleImageButton sender)
        {
            UserInterfaceManager.ComponentUpdate(GuiComponentType.ComboGui, ComboGuiMessage.ToggleShowPage, 2);
        }

        void inventoryButton_Clicked(SimpleImageButton sender)
        {
            UserInterfaceManager.ComponentUpdate(GuiComponentType.ComboGui, ComboGuiMessage.ToggleShowPage, 1);
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
            IoCManager.Resolve<IMapManager>().OnTileChanged -= OnTileChanged;
            RenderTargetCache.DestroyAll();
            GC.Collect();
        }

        public void Update( FrameEventArgs e )
        {
            LastUpdate = Now;
            Now = DateTime.Now;

            ComponentManager.Singleton.Update(e.FrameDeltaTime);
            PlacementManager.Update(MousePosScreen, MapManager);
            PlayerManager.Update(e.FrameDeltaTime);
            if(PlayerManager != null && PlayerManager.ControlledEntity != null)
                ClientWindowData.Singleton.UpdateViewPort(PlayerManager.ControlledEntity.Position);

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

        #endregion

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            var channel = (ChatChannel)msg.ReadByte();
            var text = msg.ReadString();
            var entityId = msg.ReadInt32();
            string message;
            switch (channel)
            {
                /*case ChatChannel.Emote:
                    message = _entityManager.GetEntity(entityId).Name + " " + text;
                    break;
                case ChatChannel.Damage:
                    message = text;
                    break; //Formatting is handled by the server. */
                case ChatChannel.Ingame:
                case ChatChannel.Server:
                case ChatChannel.OOC:
                case ChatChannel.Radio:
                    message = "[" + channel + "] " + text;
                    break;
                default:
                    message = text;
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

            //Gorgon.Screen.DefaultView.Left = 400;
            //Gorgon.Screen.DefaultView.Top = 400;

            //CalculateAllLights();

            if (PlayerManager.ControlledEntity != null)
            {
                var centerTile = MapManager.GetTileArrayPositionFromWorldPosition(PlayerManager.ControlledEntity.Position);

                var xStart = Math.Max(0, centerTile.X - (ScreenWidthTiles / 2) - 1);
                var yStart = Math.Max(0, centerTile.Y - (ScreenHeightTiles / 2) - 1);
                var xEnd = Math.Min(xStart + ScreenWidthTiles + 2, MapManager.GetMapWidth() - 1);
                var yEnd = Math.Min(yStart + ScreenHeightTiles + 2, MapManager.GetMapHeight() - 1);

                // Get nearby lights
                var lights =
                    IoCManager.Resolve<ILightManager>().LightsIntersectingRect(ClientWindowData.Singleton.ViewPort);

                // Render the lightmap
                RenderLightMap(lights);
                CalculateSceneBatches(xStart, xEnd, yStart, yEnd, centerTile);

                if (_redrawTiles)
                {
                    //Set rendertarget to draw the rest of the scene
                    Gorgon.CurrentRenderTarget = _tilesTarget;
                    Gorgon.CurrentRenderTarget.Clear(Color.Black);

                    if (_floorBatch.Count > 0)
                        _floorBatch.Draw();

                    if (_wallBatch.Count > 0)
                        _wallBatch.Draw();

                    _redrawTiles = false;
                }


                Gorgon.CurrentRenderTarget = _sceneTarget;
                _sceneTarget.Clear(Color.Black);

                _tilesTarget.Image.Blit(0,0, _tilesTarget.Width, _tilesTarget.Height, Color.White, BlitterSizeMode.Crop);

                ComponentManager.Singleton.Render(0, ClientWindowData.Singleton.ViewPort);

                if (_redrawOverlay)
                {
                    Gorgon.CurrentRenderTarget = _overlayTarget;
                    _overlayTarget.Clear(Color.Transparent);

                    // Render decal batch

                    if (_decalBatch.Count > 0)
                        _decalBatch.Draw();

                    if (_wallTopsBatch.Count > 0)
                        _wallTopsBatch.Draw();

                    if (_gasBatch.Count > 0)
                        _gasBatch.Draw();

                    Gorgon.CurrentRenderTarget = _sceneTarget;
                    _redrawOverlay = false;
                }

                _overlayTarget.Blit();

                LightScene();
                //Render the placement manager shit
                PlacementManager.Render();
            }
        }

        private void CalculateAllLights()
        {
            foreach(ILight l in IoCManager.Resolve<ILightManager>().GetLights().Where(l => l.LightArea.Calculated == false))
            {
                CalculateLightArea(l);
            }
        }

        private void CalculateSceneBatches(int xStart, int xEnd, int yStart, int yEnd, Point centerTile)
        {
            if (!_recalculateScene)
                return;

            // Render the player sightline occluder
            RenderPlayerVisionMap();

            //Blur the player vision map
            BlurPlayerVision();

            _decalBatch.Clear();
            _wallTopsBatch.Clear();
            _floorBatch.Clear();
            _wallBatch.Clear();
            _gasBatch.Clear();

            DrawTiles(xStart, xEnd, yStart, yEnd);
            _recalculateScene = false;
            _redrawTiles = true;
            _redrawOverlay = true;
        }

        private void RecalculateScene()
        {
            _recalculateScene = true;
        }

        private void BlurShadowMap()
        {
            _gaussianBlur.SetRadius(11);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Size(screenShadows.Width, screenShadows.Height));
            _gaussianBlur.PerformGaussianBlur(screenShadows);
        }

        private void BlurPlayerVision()
        {
            _gaussianBlur.SetRadius(11);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Size(playerOcclusionTarget.Width, playerOcclusionTarget.Height));
            _gaussianBlur.PerformGaussianBlur(playerOcclusionTarget);
        }

        private void LightScene()
        {
            //Blur the light/shadow map
            BlurShadowMap();
            
            //Render the scene and lights together to compose the lit scene
            Gorgon.CurrentRenderTarget = _composedSceneTarget;
            Gorgon.CurrentRenderTarget.Clear(Color.Black);
            Gorgon.CurrentShader = finalBlendShader.Techniques["FinalLightBlend"];
            finalBlendShader.Parameters["PlayerViewTexture"].SetValue(playerOcclusionTarget);
            Sprite outofview = IoCManager.Resolve<IResourceManager>().GetSprite("outofview");
            finalBlendShader.Parameters["OutOfViewTexture"].SetValue(outofview.Image);
            var texratiox = Gorgon.CurrentClippingViewport.Width / outofview.Width;
            var texratioy = Gorgon.CurrentClippingViewport.Height / outofview.Height;
            var maskProps = new Vector4D(texratiox, texratioy, 0, 0);
            finalBlendShader.Parameters["MaskProps"].SetValue(maskProps);
            finalBlendShader.Parameters["LightTexture"].SetValue(screenShadows);
            finalBlendShader.Parameters["SceneTexture"].SetValue(_sceneTarget);
            finalBlendShader.Parameters["AmbientLight"].SetValue(new Vector4D(.05f, .05f, 0.05f, 1));
            screenShadows.Image.Blit(0, 0, screenShadows.Width, screenShadows.Height, Color.White, BlitterSizeMode.Crop); 
            
            // Blit the shadow image on top of the screen
            Gorgon.CurrentShader = null;
            Gorgon.CurrentRenderTarget = null;

            PlayerPostProcess();

            _composedSceneTarget.Image.Blit(0,0, Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height, Color.White, BlitterSizeMode.Crop);
            //screenShadows.Blit(0,0);
            //playerOcclusionTarget.Blit(0,0);
        }

        private void PlayerPostProcess()
        {
            PlayerManager.ApplyEffects(_composedSceneTarget);
        }

        #region Lighting
        /// <summary>
        /// Renders a set of lights into a single lightmap.
        /// If a light hasn't been prerendered yet, it renders that light.
        /// </summary>
        /// <param name="lights">Array of lights</param>
        private void RenderLightMap(ILight[] lights)
        {
            //Step 1 - Calculate lights that haven't been calculated yet or need refreshing
            foreach (ILight l in lights.Where(l => l.LightArea.Calculated == false))
            {
                if (l.LightState != LightState.On)
                    continue;
                //Render the light area to its own target.
                CalculateLightArea(l);
            }

            //Step 2 - Set up the render targets for the composite lighting.
            RenderImage source = screenShadows;
            source.Clear(Color.FromArgb(0,0,0,0));
            RenderImage destination = shadowIntermediate;
            Gorgon.CurrentRenderTarget = destination;
            RenderImage copy;

            //Reset the shader and render target
            Gorgon.CurrentShader = lightMapShader.Techniques["PreLightBlend"];

            var lightTextures = new List<GorgonLibrary.Graphics.Image>();
            var colors = new List<Vector4D>();
            var positions = new List<Vector4D>();

            //Step 3 - Blend all the lights!
            foreach (ILight l in lights)
            {
                //Skip off or broken lights (TODO code broken light states)
                if (l.LightState != LightState.On)
                    continue;
                
                // LIGHT BLEND STAGE 1 - SIZING -- copys the light texture to a full screen rendertarget
                var area = (LightArea) l.LightArea;

                Vector2D blitPos;
                //Set the drawing position.
                blitPos = ClientWindowData.WorldToScreen(area.LightPosition - area.LightAreaSize * 0.5f);

                //Set shader parameters
                var LightPositionData = new Vector4D(blitPos.X/source.Width,
                                                     blitPos.Y/source.Height,
                                                     (float) source.Width/area.renderTarget.Width,
                                                     (float) source.Height/area.renderTarget.Height);
                lightTextures.Add(area.renderTarget.Image);
                colors.Add(l.GetColorVec());
                positions.Add(LightPositionData);
            }
            var i = 0;
            var num_lights = 6;
            bool draw = false;
            bool fill = false;
            var black = IoCManager.Resolve<IResourceManager>().GetSprite("black5x5").Image;
            GorgonLibrary.Graphics.Image[] r_img = new GorgonLibrary.Graphics.Image[num_lights];
            Vector4D[] r_col = new Vector4D[num_lights];
            Vector4D[] r_pos = new Vector4D[num_lights];
            do
            {
                if(fill)
                {
                    for(int j = i;j < num_lights - 1;j++)
                    {
                        r_img[j] = black;
                        r_col[j] = Vector4D.Zero;
                        r_pos[j] = new Vector4D(0, 0, 1, 1);
                        j++;
                    }
                    i = num_lights;
                    draw = true;
                    fill = false;
                }
                if(draw)
                {
                    Gorgon.CurrentRenderTarget = destination;

                    lightMapShader.Parameters["LightPosData"].SetValue(r_pos);
                    lightMapShader.Parameters["Colors"].SetValue(r_col);
                    lightMapShader.Parameters["light1"].SetValue(r_img[0]);
                    lightMapShader.Parameters["light2"].SetValue(r_img[1]);
                    lightMapShader.Parameters["light3"].SetValue(r_img[2]);
                    lightMapShader.Parameters["light4"].SetValue(r_img[3]);
                    lightMapShader.Parameters["light5"].SetValue(r_img[4]);
                    lightMapShader.Parameters["light6"].SetValue(r_img[5]);
                    lightMapShader.Parameters["SceneTexture"].SetValue(source.Image);

                    source.Image.Blit(0, 0, screenShadows.Width, screenShadows.Height, Color.White, BlitterSizeMode.Crop); // Blit the shadow image on top of the screen

                    //Swap rendertargets to set up for the next light
                    copy = source;
                    source = destination;
                    destination = copy;
                    i = 0;
                    draw = false;
                    r_img = new GorgonLibrary.Graphics.Image[num_lights];
                    r_col = new Vector4D[num_lights];
                    r_pos = new Vector4D[num_lights];
                }
                if(lightTextures.Count > 0)
                {
                    var l = lightTextures[0];
                    lightTextures.RemoveAt(0);
                    r_img[i] = l;
                    r_col[i] = colors[0];
                    colors.RemoveAt(0);
                    r_pos[i] = positions[0];
                    positions.RemoveAt(0);
                    i++;
                }
                if (i == num_lights)
                    draw = true;
                if (i > 0 && i < num_lights && lightTextures.Count == 0)
                    fill = true;
            } while (lightTextures.Count > 0 || draw || fill);

            Gorgon.CurrentShader = null;
            if(source != screenShadows)
            {
                Gorgon.CurrentRenderTarget = screenShadows;
                source.Image.Blit(0,0, source.Width, source.Height, Color.White, BlitterSizeMode.Crop);
            }
            Gorgon.CurrentRenderTarget = null;
        }

        private void RenderPlayerVisionMap()
        {
            Vector2D blitPos;
            if (bPlayerVision)
            {
                playerOcclusionTarget.Clear(Color.Black);
                playerVision.Move(PlayerManager.ControlledEntity.Position);
                LightArea area = GetLightArea(RadiusToShadowMapSize(playerVision.Radius));
                area.LightPosition = playerVision.Position; // Set the light position

                if (MapManager.GetTileTypeFromWorldPosition(playerVision.Position).GetType() == typeof(Wall))
                {
                    area.LightPosition = new Vector2D(area.LightPosition.X, MapManager.GetTileAt(playerVision.Position).Position.Y + MapManager.GetTileSpacing() + 1);
                }

                area.BeginDrawingShadowCasters(); // Start drawing to the light rendertarget
                DrawWallsRelativeToLight(area); // Draw all shadowcasting stuff here in black
                area.EndDrawingShadowCasters(); // End drawing to the light rendertarget
                shadowMapResolver.ResolveShadows(area.renderTarget.Image, area.renderTarget, area.LightPosition, false, 
                    IoCManager.Resolve<IResourceManager>().GetSprite("whitemask").Image, Vector4D.Zero, new Vector4D(1,1,1,1)); // Calc shadows

                Gorgon.CurrentRenderTarget = playerOcclusionTarget; // Set to shadow rendertarget

                blitPos = ClientWindowData.WorldToScreen(area.LightPosition - area.LightAreaSize*0.5f);
                area.renderTarget.SourceBlend = AlphaBlendOperation.One; //Additive blending
                area.renderTarget.DestinationBlend = AlphaBlendOperation.Zero; //Additive blending
                area.renderTarget.Blit(blitPos.X, blitPos.Y, area.renderTarget.Width,
                area.renderTarget.Height, Color.White, BlitterSizeMode.Crop); // Draw the lights effects
                area.renderTarget.SourceBlend = AlphaBlendOperation.SourceAlpha; //reset blend mode
                area.renderTarget.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha; //reset blend mode
            }
            else
            {
                playerOcclusionTarget.Clear(Color.White);
            }
        }

        private void CalculateLightArea(ILight l)
        {
            var area = l.LightArea;
            if (area.Calculated)
                return;
            area.LightPosition = l.Position;//mousePosWorld; // Set the light position
            if (MapManager.GetTileTypeFromWorldPosition(l.Position).GetType() == typeof(Wall))
            {
                area.LightPosition = new Vector2D(area.LightPosition.X, MapManager.GetTileAt(l.Position).Position.Y + MapManager.GetTileSpacing() + 1);
            }
            area.BeginDrawingShadowCasters(); // Start drawing to the light rendertarget
            DrawWallsRelativeToLight(area); // Draw all shadowcasting stuff here in black
            area.EndDrawingShadowCasters(); // End drawing to the light rendertarget
            shadowMapResolver.ResolveShadows(area.renderTarget.Image, area.renderTarget, area.LightPosition, true, area.Mask.Image, area.MaskProps, Vector4D.Unit); // Calc shadows
            area.Calculated = true;
        }
        
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
        private void DrawWallsRelativeToLight(ILightArea area)
        {
            Point centerTile = MapManager.GetTileArrayPositionFromWorldPosition(area.LightPosition);
            var tilespacing = MapManager.GetTileSpacing();
            int xS = Math.Max(0, centerTile.X - (int)Math.Round((area.LightAreaSize.X / tilespacing) / 2));
            int yS = Math.Max(0, centerTile.Y - (int)Math.Round((area.LightAreaSize.Y / tilespacing) / 2));
            int xE = Math.Min(centerTile.X + (int)Math.Round((area.LightAreaSize.X / tilespacing) / 2), MapManager.GetMapWidth() - 1);
            int yE = Math.Min(centerTile.Y + (int)Math.Round((area.LightAreaSize.Y / tilespacing) / 2), MapManager.GetMapHeight() - 1);

            Tile t;
            for (int x = xS; x <= xE; x++)
            {
                for (int y = yS; y <= yE; y++)
                {

                    t = (Tile)MapManager.GetTileAt(x, y);

                    if (t.GetType() == typeof(Wall))
                    {
                        Vector2D pos = area.ToRelativePosition(t.Position);
                        t.RenderPos(pos.X, pos.Y, tilespacing, (int)area.LightAreaSize.X);
                    }
                }
            }
        }
        
        /// <summary>
        /// Copys all tile sprites into batches.
        /// </summary>
        /// <param name="xStart">Leftmost tile to draw</param>
        /// <param name="xEnd">Rightmost tile to draw</param>
        /// <param name="yStart">Topmost tile to draw</param>
        /// <param name="yEnd">Bottommost tile to draw</param>
        private void DrawTiles(int xStart, int xEnd, int yStart, int yEnd)
        {
            var tilespacing = MapManager.GetTileSpacing();
            Tile t;
            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    t = (Tile)MapManager.GetTileAt(x, y);

                    if (t.GetType() == typeof(Wall))
                    {
                        t.Render(WindowOrigin.X, WindowOrigin.Y, tilespacing, _wallBatch);
                    }
                    else if (t.GetType() != typeof(Wall))
                    {
                        t.Render(WindowOrigin.X, WindowOrigin.Y, tilespacing, _floorBatch);
                    }

                    // Render gas sprites to gas batch
                    t.RenderGas(WindowOrigin.X, WindowOrigin.Y, tilespacing, _gasBatch);

                    t.RenderTop(WindowOrigin.X, WindowOrigin.Y, tilespacing, _wallTopsBatch);
                }
            }
        }

        public void OnTileChanged(Point tilePosition, PointF tileWorldPosition)
        {
            var ts = IoCManager.Resolve<IMapManager>().GetTileSpacing();
            IoCManager.Resolve<ILightManager>().RecalculateLightsInView(new RectangleF(tileWorldPosition, new SizeF(ts, ts)));

            // Recalculate the scene batches.
            RecalculateScene();
        }   

        #endregion
       
        public void FormResize()
        {
            Gorgon.CurrentClippingViewport = new Viewport(0, 0, Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);
            ClientWindowData.Singleton.UpdateViewPort(PlayerManager.ControlledEntity.Position);
            UserInterfaceManager.ResizeComponents();
            ResetRendertargets();
            IoCManager.Resolve<ILightManager>().RecalculateLights();
            RecalculateScene();
            
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
                lights[i].LightArea.Calculated = false;
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
            if (e.Key == KeyboardKeys.Escape)
            {
                UserInterfaceManager.DisposeAllComponents<MenuWindow>(); //Remove old ones.
                UserInterfaceManager.AddComponent(new MenuWindow()); //Create a new one.
            }
            if (e.Key == KeyboardKeys.F9)
            {
                UserInterfaceManager.ToggleMoveMode();
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
                UserInterfaceManager.DisposeAllComponents<PlayerActionsWindow>(); //Remove old ones.
                PlayerActionComp actComp = (PlayerActionComp)PlayerManager.ControlledEntity.GetComponent(ComponentFamily.PlayerActions);
                if (actComp != null)
                    UserInterfaceManager.AddComponent(new PlayerActionsWindow(new Size(150, 150), ResourceManager, actComp)); //Create a new one.
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
                //var entToClick = (from cd in clickedEntities                       //Treat mobs and their clothes as on the same level as ground placeables (windows, doors)
                //                  orderby (cd.Drawdepth == (int)DrawDepth.MobBase ||//This is a workaround to make both windows etc. and objects that rely on layers (objects on tables) work.
                //                            cd.Drawdepth == (int)DrawDepth.MobOverAccessoryLayer ||
                //                            cd.Drawdepth == (int)DrawDepth.MobOverClothingLayer ||
                //                            cd.Drawdepth == (int)DrawDepth.MobUnderAccessoryLayer ||
                //                            cd.Drawdepth == (int)DrawDepth.MobUnderClothingLayer
                //                   ? (int)DrawDepth.FloorPlaceable : cd.Drawdepth) ascending, cd.Clicked.Position.Y ascending
                //                  select cd.Clicked).Last();

                var entToClick = (from cd in clickedEntities
                                  orderby cd.Drawdepth ascending, cd.Clicked.Position.Y ascending
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

                    case MouseButtons.Middle:
                        UserInterfaceManager.DisposeAllComponents<PropEditWindow>();
                        UserInterfaceManager.AddComponent(new PropEditWindow(new Size(400, 400), ResourceManager, (Entity)entToClick));
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

        private void OnPlayerMove(object sender, VectorEventArgs args)
        {
            //Recalculate scene batches for drawing.
            RecalculateScene();
        }
    }

}