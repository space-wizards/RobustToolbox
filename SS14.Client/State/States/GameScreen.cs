using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Lighting;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Helpers;
using SS14.Client.Lighting;
using SS14.Client.UserInterface.Components;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameStates;
using SS14.Shared.IoC;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Network;
using SS14.Shared.Utility;
using KeyEventArgs = SFML.Window.KeyEventArgs;
using Vector2i = SFML.System.Vector2i;

namespace SS14.Client.State.States
{
    public class GameScreen : State, IState
    {
        #region Variables
        public DateTime LastUpdate;
        public DateTime Now;

        public int ScreenHeightTiles = 12;
        public int ScreenWidthTiles = 15; // How many tiles around us do we draw?
        public string SpawnType;

        private float _realScreenHeightTiles;
        private float _realScreenWidthTiles;

        private bool _recalculateScene = true;
        private bool _redrawOverlay = true;
        private bool _redrawTiles = true;
        private bool _showDebug; // show AABBs & Bounding Circles on Entities.

        private RenderImage _baseTarget;
        private Sprite _baseTargetSprite;

        private IClientEntityManager _entityManager;
        private IComponentManager _componentManager;

        private GaussianBlur _gaussianBlur;

        private List<RenderImage> _cleanupList = new List<RenderImage>();
        private List<Sprite> _cleanupSpriteList = new List<Sprite>();

        private SpriteBatch _wallBatch;
        private SpriteBatch _wallTopsBatch;
        private SpriteBatch _floorBatch;
        private SpriteBatch _gasBatch;
        private SpriteBatch _decalBatch;

        #region gameState stuff
        private readonly Dictionary<uint, GameState> _lastStates = new Dictionary<uint, GameState>();
        private uint _currentStateSequence; //We only ever want a newer state than the current one
        #endregion gameState stuff

        #region Mouse/Camera stuff
        public Vector2i MousePosScreen = new Vector2i();
        public Vector2f MousePosWorld = new Vector2f();
        #endregion Mouse/Camera stuff

        #region UI Variables
        private int _prevScreenWidth = 0;
        private int _prevScreenHeight = 0;

        private MenuWindow _menu;
        private Chatbox _gameChat;
        #endregion UI Variables

        #region Lighting
        private Sprite _lightTargetIntermediateSprite;
        private Sprite _lightTargetSprite;

        private bool bPlayerVision = false;
        private bool bFullVision = false;
        private bool debugWallOccluders = false;
        private bool debugPlayerShadowMap = false;
        private bool debugHitboxes = false;
        public bool BlendLightMap = true;

        private TechniqueList LightblendTechnique;
        private GLSLShader Lightmap;

        private LightArea lightArea1024;
        private LightArea lightArea128;
        private LightArea lightArea256;
        private LightArea lightArea512;

        private ILight playerVision;
        private ISS14Serializer serializer;

        private RenderImage playerOcclusionTarget;
        private RenderImage _occluderDebugTarget;
        private RenderImage _lightTarget;
        private RenderImage _lightTargetIntermediate;
        private RenderImage _composedSceneTarget;
        private RenderImage _overlayTarget;
        private RenderImage _sceneTarget;
        private RenderImage _tilesTarget;
        private RenderImage screenShadows;
        private RenderImage shadowBlendIntermediate;
        private RenderImage shadowIntermediate;

        //private QuadRenderer quadRenderer;
        private ShadowMapResolver shadowMapResolver;

        #endregion Lighting

        #endregion Variables

        public GameScreen(IDictionary<Type, object> managers) : base(managers)
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // Disable lighting on non-Windows versions by default because the rendering is broken.
                bFullVision = true;
            }
        }

        #region IState Members

        public void Startup()
        {
            var manager = IoCManager.Resolve<IConfigurationManager>();
            manager.RegisterCVar("player.name", "Joe Genero", CVarFlags.ARCHIVE);

            LastUpdate = DateTime.Now;
            Now = DateTime.Now;

            _cleanupList = new List<RenderImage>();
            _cleanupSpriteList = new List<Sprite>();

            UserInterfaceManager.DisposeAllComponents();

            //Init serializer
            serializer = IoCManager.Resolve<ISS14Serializer>();

            _entityManager = IoCManager.Resolve<IClientEntityManager>();
            _componentManager = IoCManager.Resolve<IComponentManager>();
            IoCManager.Resolve<IMapManager>().TileChanged += OnTileChanged;
            IoCManager.Resolve<IPlayerManager>().OnPlayerMove += OnPlayerMove;

            NetworkManager.MessageArrived += NetworkManagerMessageArrived;

            NetOutgoingMessage message = NetworkManager.Peer.CreateMessage();
            message.Write((byte)NetMessages.RequestMap);
            NetworkManager.ClientSendMessage(message, NetDeliveryMethod.ReliableUnordered);

            // TODO This should go somewhere else, there should be explicit session setup and teardown at some point.
            var message1 = NetworkManager.Peer.CreateMessage();
            message1.Write((byte)NetMessages.ClientName);
            message1.Write(ConfigurationManager.GetCVar<string>("player.name"));
            NetworkManager.ClientSendMessage(message1, NetDeliveryMethod.ReliableOrdered);

            // Create new
            _gaussianBlur = new GaussianBlur(ResourceCache);

            _realScreenWidthTiles = (float)CluwneLib.Screen.Size.X / MapManager.TileSize;
            _realScreenHeightTiles = (float)CluwneLib.Screen.Size.Y / MapManager.TileSize;

            InitializeRenderTargets();
            InitializeSpriteBatches();
            InitalizeLighting();
            InitializeGUI();
        }

        private void InitializeRenderTargets()
        {
            _baseTarget = new RenderImage("baseTarget", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y, true);
            _cleanupList.Add(_baseTarget);

            _baseTargetSprite = new Sprite(_baseTarget.Texture);
            _cleanupSpriteList.Add(_baseTargetSprite);

            _sceneTarget = new RenderImage("sceneTarget", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y, true);
            _cleanupList.Add(_sceneTarget);
            _tilesTarget = new RenderImage("tilesTarget", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y, true);
            _cleanupList.Add(_tilesTarget);

            _overlayTarget = new RenderImage("OverlayTarget", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y, true);
            _cleanupList.Add(_overlayTarget);

            //_overlayTarget.SourceBlend = AlphaBlendOperation.SourceAlpha;
            //_overlayTarget.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha;
            //_overlayTarget.SourceBlendAlpha = AlphaBlendOperation.SourceAlpha;
            //_overlayTarget.DestinationBlendAlpha = AlphaBlendOperation.InverseSourceAlpha;

            _overlayTarget.BlendSettings.ColorSrcFactor = BlendMode.Factor.SrcAlpha;
            _overlayTarget.BlendSettings.ColorDstFactor = BlendMode.Factor.OneMinusSrcAlpha;
            _overlayTarget.BlendSettings.AlphaSrcFactor = BlendMode.Factor.SrcAlpha;
            _overlayTarget.BlendSettings.AlphaDstFactor = BlendMode.Factor.OneMinusSrcAlpha;

            _composedSceneTarget = new RenderImage("composedSceneTarget", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y,
                                                 ImageBufferFormats.BufferRGB888A8);
            _cleanupList.Add(_composedSceneTarget);

            _lightTarget = new RenderImage("lightTarget", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y, ImageBufferFormats.BufferRGB888A8);

            _cleanupList.Add(_lightTarget);
            _lightTargetSprite = new Sprite(_lightTarget.Texture);

            _cleanupSpriteList.Add(_lightTargetSprite);

            _lightTargetIntermediate = new RenderImage("lightTargetIntermediate", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y,
                                                      ImageBufferFormats.BufferRGB888A8);
            _cleanupList.Add(_lightTargetIntermediate);
            _lightTargetIntermediateSprite = new Sprite(_lightTargetIntermediate.Texture);
            _cleanupSpriteList.Add(_lightTargetIntermediateSprite);
        }

        private void InitializeSpriteBatches()
        {
            _gasBatch = new SpriteBatch();
            //_gasBatch.SourceBlend                   = AlphaBlendOperation.SourceAlpha;
            //_gasBatch.DestinationBlend              = AlphaBlendOperation.InverseSourceAlpha;
            //_gasBatch.SourceBlendAlpha              = AlphaBlendOperation.SourceAlpha;
            //_gasBatch.DestinationBlendAlpha         = AlphaBlendOperation.InverseSourceAlpha;
            _gasBatch.BlendingSettings.ColorSrcFactor = BlendMode.Factor.SrcAlpha;
            _gasBatch.BlendingSettings.ColorDstFactor = BlendMode.Factor.OneMinusDstAlpha;
            _gasBatch.BlendingSettings.AlphaSrcFactor = BlendMode.Factor.SrcAlpha;
            _gasBatch.BlendingSettings.AlphaDstFactor = BlendMode.Factor.OneMinusSrcAlpha;

            _wallTopsBatch = new SpriteBatch();
            //_wallTopsBatch.SourceBlend                   = AlphaBlendOperation.SourceAlpha;
            //_wallTopsBatch.DestinationBlend              = AlphaBlendOperation.InverseSourceAlpha;
            //_wallTopsBatch.SourceBlendAlpha              = AlphaBlendOperation.SourceAlpha;
            //_wallTopsBatch.DestinationBlendAlpha         = AlphaBlendOperation.InverseSourceAlpha;
            _wallTopsBatch.BlendingSettings.ColorSrcFactor = BlendMode.Factor.SrcAlpha;
            _wallTopsBatch.BlendingSettings.ColorDstFactor = BlendMode.Factor.OneMinusDstAlpha;
            _wallTopsBatch.BlendingSettings.AlphaSrcFactor = BlendMode.Factor.SrcAlpha;
            _wallTopsBatch.BlendingSettings.AlphaDstFactor = BlendMode.Factor.OneMinusSrcAlpha;

            _decalBatch = new SpriteBatch();
            //_decalBatch.SourceBlend                   = AlphaBlendOperation.SourceAlpha;
            //_decalBatch.DestinationBlend              = AlphaBlendOperation.InverseSourceAlpha;
            //_decalBatch.SourceBlendAlpha              = AlphaBlendOperation.SourceAlpha;
            //_decalBatch.DestinationBlendAlpha         = AlphaBlendOperation.InverseSourceAlpha;
            _decalBatch.BlendingSettings.ColorSrcFactor = BlendMode.Factor.SrcAlpha;
            _decalBatch.BlendingSettings.ColorDstFactor = BlendMode.Factor.OneMinusDstAlpha;
            _decalBatch.BlendingSettings.AlphaSrcFactor = BlendMode.Factor.SrcAlpha;
            _decalBatch.BlendingSettings.AlphaDstFactor = BlendMode.Factor.OneMinusSrcAlpha;

            _floorBatch = new SpriteBatch();
            _wallBatch = new SpriteBatch();
        }

        private Vector2i _gameChatSize = new Vector2i(475, 175); // TODO: Move this magic variable

        private void UpdateGUIPosition()
        {
            _gameChat.Position = new Vector2i((int)CluwneLib.Screen.Size.X - _gameChatSize.X - 10, 10);
        }

        private void InitializeGUI()
        {
            // Setup the ESC Menu
            _menu = new MenuWindow();
            UserInterfaceManager.AddComponent(_menu);
            _menu.SetVisible(false);

            //Init GUI components
            _gameChat = new Chatbox("gamechat", _gameChatSize, ResourceCache);
            _gameChat.TextSubmitted += ChatTextboxTextSubmitted;
            UserInterfaceManager.AddComponent(_gameChat);
        }

        private void InitalizeLighting()
        {
            shadowMapResolver = new ShadowMapResolver(ShadowmapSize.Size1024, ShadowmapSize.Size1024,
                                                      ResourceCache);
            shadowMapResolver.LoadContent();
            lightArea128 = new LightArea(ShadowmapSize.Size128);
            lightArea256 = new LightArea(ShadowmapSize.Size256);
            lightArea512 = new LightArea(ShadowmapSize.Size512);
            lightArea1024 = new LightArea(ShadowmapSize.Size1024);

            screenShadows = new RenderImage("screenShadows", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y, ImageBufferFormats.BufferRGB888A8);

            _cleanupList.Add(screenShadows);
            screenShadows.UseDepthBuffer = false;
            shadowIntermediate = new RenderImage("shadowIntermediate", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y,
                                                 ImageBufferFormats.BufferRGB888A8);
            _cleanupList.Add(shadowIntermediate);
            shadowIntermediate.UseDepthBuffer = false;
            shadowBlendIntermediate = new RenderImage("shadowBlendIntermediate", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y,
                                                      ImageBufferFormats.BufferRGB888A8);
            _cleanupList.Add(shadowBlendIntermediate);
            shadowBlendIntermediate.UseDepthBuffer = false;
            playerOcclusionTarget = new RenderImage("playerOcclusionTarget", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y,
                                                    ImageBufferFormats.BufferRGB888A8);
            _cleanupList.Add(playerOcclusionTarget);
            playerOcclusionTarget.UseDepthBuffer = false;

            LightblendTechnique = IoCManager.Resolve<IResourceCache>().GetTechnique("lightblend");
            Lightmap = IoCManager.Resolve<IResourceCache>().GetShader("lightmap");

            playerVision = IoCManager.Resolve<ILightManager>().CreateLight();
            playerVision.SetColor(Color.Blue);
            playerVision.SetRadius(1024);
            playerVision.Move(new Vector2f());

            _occluderDebugTarget = new RenderImage("debug", CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y);
        }

        public void Update(FrameEventArgs e)
        {
            LastUpdate = Now;
            Now = DateTime.Now;

            if (CluwneLib.Screen.Size.X != _prevScreenWidth || CluwneLib.Screen.Size.Y != _prevScreenHeight)
            {
                _prevScreenHeight = (int)CluwneLib.Screen.Size.Y;
                _prevScreenWidth = (int)CluwneLib.Screen.Size.X;
                UpdateGUIPosition();
            }

            CluwneLib.TileSize = MapManager.TileSize;

            _componentManager.Update(e.FrameDeltaTime);
            _entityManager.Update(e.FrameDeltaTime);
            PlacementManager.Update(MousePosScreen, MapManager);
            PlayerManager.Update(e.FrameDeltaTime);

            if (PlayerManager.ControlledEntity != null)
            {
                CluwneLib.WorldCenter = PlayerManager.ControlledEntity.GetComponent<ITransformComponent>().Position.Convert();
                MousePosWorld = CluwneLib.ScreenToWorld(MousePosScreen); // Use WorldCenter to calculate, so we need to update again
            }
        }

        public void Render(FrameEventArgs e)
        {
            CluwneLib.Screen.Clear(Color.Black);
            CluwneLib.TileSize = MapManager.TileSize;

            CalculateAllLights();

            if (PlayerManager.ControlledEntity != null)
            {
                CluwneLib.ScreenViewportSize = new Vector2u(CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y);
                var vp = CluwneLib.WorldViewport;

                ILight[] lights = IoCManager.Resolve<ILightManager>().LightsIntersectingRect(vp);

                // Render the lightmap
                RenderLightsIntoMap(lights);
                CalculateSceneBatches(vp);

                //Draw all rendertargets to the scenetarget
                _sceneTarget.BeginDrawing();
                _sceneTarget.Clear(Color.Black);

                //PreOcclusion
                RenderTiles();

                //ComponentManager.Singleton.Render(0, CluwneLib.ScreenViewport);
                RenderComponents(e.FrameDeltaTime, vp);

                RenderOverlay();

                _sceneTarget.EndDrawing();
                _sceneTarget.ResetCurrentRenderTarget();
                //_sceneTarget.Blit(0, 0, CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y);

                //Debug.DebugRendertarget(_sceneTarget);

                if (bFullVision)
                    _sceneTarget.Blit(0, 0, CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y);
                else
                    LightScene();

                RenderDebug(vp);

                //Render the placement manager shit
                PlacementManager.Render();
            }
        }

        private void RenderTiles()
        {
            if (_redrawTiles)
            {
                //Set rendertarget to draw the rest of the scene
                _tilesTarget.BeginDrawing();
                _tilesTarget.Clear(Color.Black);

                if (_floorBatch.Count > 0)
                {
                    _tilesTarget.Draw(_floorBatch);
                }

                if (_wallBatch.Count > 0)
                    _tilesTarget.Draw(_wallBatch);

                if (_wallTopsBatch.Count > 0)
                    _overlayTarget.Draw(_wallTopsBatch);

                _tilesTarget.EndDrawing();
                _redrawTiles = false;
            }

            _tilesTarget.Blit(0, 0, _tilesTarget.Width, _tilesTarget.Height, Color.White, BlitterSizeMode.Scale);
        }

        private void RenderOverlay()
        {
            if (_redrawOverlay)
            {
                _overlayTarget.BeginDrawing();
                _overlayTarget.Clear(Color.Transparent);

                // Render decal batch

                if (_decalBatch.Count > 0)
                    _overlayTarget.Draw(_decalBatch);

                if (_gasBatch.Count > 0)
                    _overlayTarget.Draw(_gasBatch);

                _redrawOverlay = false;
                _overlayTarget.EndDrawing();
            }

            _overlayTarget.Blit(0, 0, _tilesTarget.Width, _tilesTarget.Height, Color.White, BlitterSizeMode.Crop);
        }

        private void RenderDebug(FloatRect viewport)
        {
            if (debugWallOccluders || debugPlayerShadowMap)
                _occluderDebugTarget.Blit(0, 0, _occluderDebugTarget.Width / 4, _occluderDebugTarget.Height / 4, Color.White, BlitterSizeMode.Scale);

            if (CluwneLib.Debug.DebugColliders)
            {
                var colliders =
                    _componentManager.GetComponents<CollidableComponent>()
                    .Select(c => new { Color = c.DebugColor, AABB = c.WorldAABB })
                    .Where(c => !c.AABB.IsEmpty() && c.AABB.Intersects(viewport));

                var collidables =
                    _componentManager.GetComponents<CollidableComponent>()
                    .Select(c => new { Color = c.DebugColor, AABB = c.AABB })
                    .Where(c => !c.AABB.IsEmpty() && c.AABB.Intersects(viewport));

                foreach (var hitbox in colliders.Concat(collidables))
                {
                    var box = CluwneLib.WorldToScreen(hitbox.AABB);
                    CluwneLib.drawRectangle((int)box.Left, (int)box.Top, (int)box.Width, (int)box.Height,
                        hitbox.Color.WithAlpha(64));
                    CluwneLib.drawHollowRectangle((int)box.Left, (int)box.Top, (int)box.Width, (int)box.Height, 1f,
                        hitbox.Color.WithAlpha(128));
                }
            }
            if (CluwneLib.Debug.DebugGridDisplay)
            {
                int startX = 10;
                int startY = 10;
                CluwneLib.drawRectangle(startX, startY, 200, 300,
                        Color.Blue.WithAlpha(64));

                // Player position debug
                Vector2f playerWorldOffset = PlayerManager.ControlledEntity.GetComponent<ITransformComponent>().Position.Convert();
                Vector2f playerTile = CluwneLib.WorldToTile(playerWorldOffset);
                Vector2f playerScreen = CluwneLib.WorldToScreen(playerWorldOffset);
                CluwneLib.drawText(15, 15, "Postioning Debug", 14, Color.White);
                CluwneLib.drawText(15, 30, "Character Pos", 14, Color.White);
                CluwneLib.drawText(15, 45, String.Format("Pixel: {0} / {1}", playerWorldOffset.X, playerWorldOffset.Y), 14, Color.White);
                CluwneLib.drawText(15, 60, String.Format("World: {0} / {1}", playerTile.X, playerTile.Y), 14, Color.White);
                CluwneLib.drawText(15, 75, String.Format("Screen: {0} / {1}", playerScreen.X, playerScreen.Y), 14, Color.White);

                // Mouse position debug
                Vector2i mouseScreenPos = MousePosScreen; // default to screen space
                Vector2f mouseWorldOffset = CluwneLib.ScreenToWorld(MousePosScreen);
                Vector2f mouseTile = CluwneLib.WorldToTile(mouseWorldOffset);
                CluwneLib.drawText(15, 120, "Mouse Pos", 14, Color.White);
                CluwneLib.drawText(15, 135, String.Format("Pixel: {0} / {1}", mouseWorldOffset.X, mouseWorldOffset.Y), 14, Color.White);
                CluwneLib.drawText(15, 150, String.Format("World: {0} / {1}", mouseTile.X, mouseTile.Y), 14, Color.White);
                CluwneLib.drawText(15, 165, String.Format("Screen: {0} / {1}", mouseScreenPos.X, mouseScreenPos.Y), 14, Color.White);
            }
        }

        public void Shutdown()
        {
            IoCManager.Resolve<IPlayerManager>().Detach();

            _cleanupSpriteList.ForEach(s => s.Texture = null);
            _cleanupSpriteList.Clear();
            _cleanupList.ForEach(t => { t.Dispose(); });
            _cleanupList.Clear();

            shadowMapResolver.Dispose();
            _gaussianBlur.Dispose();
            _entityManager.Shutdown();
            UserInterfaceManager.DisposeAllComponents();
            NetworkManager.MessageArrived -= NetworkManagerMessageArrived;
            _decalBatch.Dispose();
            _floorBatch.Dispose();
            _gasBatch.Dispose();
            _wallBatch.Dispose();
            _wallTopsBatch.Dispose();
            GC.Collect();
        }

        #endregion IState Members

        #region Input

        #region Keyboard
        public void KeyPressed(KeyEventArgs e)
        {
        }

        public void KeyDown(KeyEventArgs e)
        {
            if (UserInterfaceManager.KeyDown(e)) //KeyDown returns true if the click is handled by the ui component.
                return;

            if (e.Code == Keyboard.Key.F1)
            {
                //TODO FrameStats
                CluwneLib.FrameStatsVisible = !CluwneLib.FrameStatsVisible;
            }
            if (e.Code == Keyboard.Key.F2)
            {
                _showDebug = !_showDebug;
                CluwneLib.Debug.ToggleWallDebug();
                CluwneLib.Debug.ToggleAABBDebug();
                CluwneLib.Debug.ToggleGridDisplayDebug();
            }
            if (e.Code == Keyboard.Key.F3)
            {
                ToggleOccluderDebug();
            }
            if (e.Code == Keyboard.Key.F4)
            {
                debugHitboxes = !debugHitboxes;
            }
            if (e.Code == Keyboard.Key.F5)
            {
                PlayerManager.SendVerb("save", 0);
            }
            if (e.Code == Keyboard.Key.F6)
            {
                bFullVision = !bFullVision;
            }
            if (e.Code == Keyboard.Key.F7)
            {
                bPlayerVision = !bPlayerVision;
            }
            if (e.Code == Keyboard.Key.F8)
            {
                NetOutgoingMessage message = NetworkManager.CreateMessage();
                message.Write((byte)NetMessages.ForceRestart);
                NetworkManager.ClientSendMessage(message, NetDeliveryMethod.ReliableUnordered);
            }
            if (e.Code == Keyboard.Key.Escape)
            {
                _menu.ToggleVisible();
            }
            if (e.Code == Keyboard.Key.F9)
            {
                UserInterfaceManager.ToggleMoveMode();
            }
            if (e.Code == Keyboard.Key.F10)
            {
                UserInterfaceManager.DisposeAllComponents<TileSpawnPanel>(); //Remove old ones.
                UserInterfaceManager.AddComponent(new TileSpawnPanel(new Vector2i(350, 410), ResourceCache,
                                                                     PlacementManager)); //Create a new one.
            }
            if (e.Code == Keyboard.Key.F11)
            {
                UserInterfaceManager.DisposeAllComponents<EntitySpawnPanel>(); //Remove old ones.
                UserInterfaceManager.AddComponent(new EntitySpawnPanel(new Vector2i(350, 410), ResourceCache,
                                                                       PlacementManager)); //Create a new one.
            }

            PlayerManager.KeyDown(e.Code);
        }

        public void KeyUp(KeyEventArgs e)
        {
            PlayerManager.KeyUp(e.Code);
        }

        public void TextEntered(TextEventArgs e)
        {
            UserInterfaceManager.TextEntered(e);
        }
        #endregion Keyboard

        #region Mouse
        public void MouseUp(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseUp(e);
        }

        public void MouseDown(MouseButtonEventArgs e)
        {
            if (PlayerManager.ControlledEntity == null)
                return;

            if (UserInterfaceManager.MouseDown(e))
                // MouseDown returns true if the click is handled by the ui component.
                return;

            if (PlacementManager.IsActive && !PlacementManager.Eraser)
            {
                switch (e.Button)
                {
                    case Mouse.Button.Left:
                        PlacementManager.HandlePlacement();
                        return;
                    case Mouse.Button.Right:
                        PlacementManager.Clear();
                        return;
                    case Mouse.Button.Middle:
                        PlacementManager.Rotate();
                        return;
                }
            }

            #region Object clicking

            // Convert our click from screen -> world coordinates
            //Vector2 worldPosition = new Vector2(e.Position.X + xTopLeft, e.Position.Y + yTopLeft);
            float checkDistance = 1.5f;
            // Find all the entities near us we could have clicked
            IEnumerable<IEntity> entities =
                _entityManager.GetEntitiesInRange(
                    PlayerManager.ControlledEntity.GetComponent<ITransformComponent>().Position.Convert(),
                    checkDistance);

            // See which one our click AABB intersected with
            var clickedEntities = new List<ClickData>();
            var clickedWorldPoint = new Vector2f(MousePosWorld.X, MousePosWorld.Y);
            foreach (IEntity entity in entities)
            {
                if (entity.TryGetComponent<IClientClickableComponent>(out var component)
                 && component.CheckClick(clickedWorldPoint, out int drawdepthofclicked))
                {
                    clickedEntities.Add(new ClickData(entity, drawdepthofclicked));
                }
            }

            if (!clickedEntities.Any())
            {
                return;
            }
            //var entToClick = (from cd in clickedEntities                       //Treat mobs and their clothes as on the same level as ground placeables (windows, doors)
            //                  orderby (cd.Drawdepth == (int)DrawDepth.MobBase ||//This is a workaround to make both windows etc. and objects that rely on layers (objects on tables) work.
            //                            cd.Drawdepth == (int)DrawDepth.MobOverAccessoryLayer ||
            //                            cd.Drawdepth == (int)DrawDepth.MobOverClothingLayer ||
            //                            cd.Drawdepth == (int)DrawDepth.MobUnderAccessoryLayer ||
            //                            cd.Drawdepth == (int)DrawDepth.MobUnderClothingLayer
            //                   ? (int)DrawDepth.FloorPlaceable : cd.Drawdepth) ascending, cd.Clicked.Position.Y ascending
            //                  select cd.Clicked).Last();

            IEntity entToClick = (from cd in clickedEntities
                                    orderby cd.Drawdepth ascending,
                                        cd.Clicked.GetComponent<ITransformComponent>().Position
                                        .Y ascending
                                    select cd.Clicked).Last();

            if (PlacementManager.Eraser && PlacementManager.IsActive)
            {
                PlacementManager.HandleDeletion(entToClick);
                return;
            }

            var clickable = entToClick.GetComponent<IClientClickableComponent>();
            switch (e.Button)
            {
                case Mouse.Button.Left:
                    clickable.DispatchClick(PlayerManager.ControlledEntity, MouseClickType.Left);
                    break;
                case Mouse.Button.Right:
                    clickable.DispatchClick(PlayerManager.ControlledEntity, MouseClickType.Right);
                    break;
                case Mouse.Button.Middle:
                    UserInterfaceManager.DisposeAllComponents<PropEditWindow>();
                    UserInterfaceManager.AddComponent(new PropEditWindow(new Vector2i(400, 400), ResourceCache,
                                                                            entToClick));
                    return;
            }

            #endregion Object clicking
        }

        public void MouseMove(MouseMoveEventArgs e)
        {
            MousePosScreen = new Vector2i(e.X, e.Y);
            MousePosWorld = CluwneLib.ScreenToWorld(MousePosScreen);
            UserInterfaceManager.MouseMove(e);
        }

        public void MouseMoved(MouseMoveEventArgs e)
        {
        }

        public void MousePressed(MouseButtonEventArgs e)
        {
        }

        public void MouseWheelMove(MouseWheelEventArgs e)
        {
            UserInterfaceManager.MouseWheelMove(e);
        }

        public void MouseEntered(EventArgs e)
        {
            UserInterfaceManager.MouseEntered(e);
        }

        public void MouseLeft(EventArgs e)
        {
            UserInterfaceManager.MouseLeft(e);
        }
        #endregion Mouse

        #region Chat
        private void HandleChatMessage(NetIncomingMessage msg)
        {
            var channel = (ChatChannel)msg.ReadByte();
            string text = msg.ReadString();
            int entityId = msg.ReadInt32();
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
            if (entityId > 0 && _entityManager.TryGetEntity(entityId, out IEntity a))
            {
                a.SendMessage(this, ComponentMessageType.EntitySaidSomething, channel, text);
            }
        }

        private void ChatTextboxTextSubmitted(Chatbox chatbox, string text)
        {
            SendChatMessage(text);
        }

        private void SendChatMessage(string text)
        {
            NetOutgoingMessage message = NetworkManager.CreateMessage();
            message.Write((byte)NetMessages.ChatMessage);
            message.Write((byte)ChatChannel.Player);
            message.Write(text);
            message.Write(-1);
            NetworkManager.ClientSendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        #endregion Chat

        #endregion Input

        #region Event Handlers

        #region Buttons
        private void menuButton_Clicked(ImageButton sender)
        {
            _menu.ToggleVisible();
        }

        private void statusButton_Clicked(ImageButton sender)
        {
            UserInterfaceManager.ComponentUpdate(GuiComponentType.ComboGui, ComboGuiMessage.ToggleShowPage, 2);
        }

        private void inventoryButton_Clicked(ImageButton sender)
        {
            UserInterfaceManager.ComponentUpdate(GuiComponentType.ComboGui, ComboGuiMessage.ToggleShowPage, 1);
        }

        #endregion Buttons

        #region Messages

        private void NetworkManagerMessageArrived(object sender, NetMessageArgs args)
        {
            NetIncomingMessage message = args.RawMessage;
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
                        string disconnectMessage = message.ReadString();
                        UserInterfaceManager.AddComponent(new DisconnectedScreenBlocker(StateManager,
                                                                                        UserInterfaceManager,
                                                                                        ResourceCache,
                                                                                        disconnectMessage));
                    }
                    break;
                case NetIncomingMessageType.Data:
                    var messageType = (NetMessages)message.ReadByte();
                    switch (messageType)
                    {
                        case NetMessages.MapMessage:
                            MapManager.HandleNetworkMessage(message);
                            break;
                        //case NetMessages.AtmosDisplayUpdate:
                        //    MapManager.HandleAtmosDisplayUpdate(message);
                        //    break;
                        case NetMessages.PlayerSessionMessage:
                            PlayerManager.HandleNetworkMessage(message);
                            break;
                        case NetMessages.PlayerUiMessage:
                            UserInterfaceManager.HandleNetMessage(message);
                            break;
                        case NetMessages.PlacementManagerMessage:
                            PlacementManager.HandleNetMessage(message);
                            break;
                        case NetMessages.ChatMessage:
                            HandleChatMessage(message);
                            break;
                        case NetMessages.EntityMessage:
                            _entityManager.HandleEntityNetworkMessage(message);
                            break;
                        case NetMessages.StateUpdate:
                            HandleStateUpdate(message);
                            break;
                        case NetMessages.FullState:
                            HandleFullState(message);
                            break;
                    }
                    break;
            }
        }

        #endregion Messages

        #region State

        /// <summary>
        /// HandleStateUpdate
        ///
        /// Receives a state update message and unpacks the delicious GameStateDelta hidden inside
        /// Then it applies the gamestatedelta to a past state to form: a full game state!
        /// </summary>
        /// <param name="message">incoming state update message</param>
        private void HandleStateUpdate(NetIncomingMessage message)
        {
            //Read the delta from the message
            GameStateDelta delta = GameStateDelta.ReadDelta(message);

            if (!_lastStates.ContainsKey(delta.FromSequence)) // Drop messages that reference a state that we don't have
                return; //TODO request full state here?

            //Acknowledge reciept before we do too much more shit -- ack as quickly as possible
            SendStateAck(delta.Sequence);

            //Grab the 'from' state
            GameState fromState = _lastStates[delta.FromSequence];
            //Apply the delta
            GameState newState = fromState + delta;
            newState.GameTime = (float) IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds;

            // Go ahead and store it even if our current state is newer than this one, because
            // a newer state delta may later reference this one.
            _lastStates[delta.Sequence] = newState;

            if (delta.Sequence > _currentStateSequence)
                _currentStateSequence = delta.Sequence;

            ApplyCurrentGameState();

            //Dump states that have passed out of being relevant
            CullOldStates(delta.FromSequence);
        }

        /// <summary>
        /// CullOldStates
        ///
        /// Deletes states that are no longer relevant
        /// </summary>
        /// <param name="sequence">state sequence number</param>
        private void CullOldStates(uint sequence)
        {
            foreach (uint v in _lastStates.Keys.Where(v => v < sequence).ToList())
                _lastStates.Remove(v);
        }

        /// <summary>
        /// HandleFullState
        ///
        /// Handles full gamestates - for initializing.
        /// </summary>
        /// <param name="message">incoming full state message</param>
        private void HandleFullState(NetIncomingMessage message)
        {
            GameState newState = GameState.ReadStateMessage(message);
            newState.GameTime = (float)IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds;
            SendStateAck(newState.Sequence);

            //Store the new state
            _lastStates[newState.Sequence] = newState;
            _currentStateSequence = newState.Sequence;
            ApplyCurrentGameState();
        }

        private void ApplyCurrentGameState()
        {
            GameState currentState = _lastStates[_currentStateSequence];
            _entityManager.ApplyEntityStates(currentState.EntityStates, currentState.GameTime);
            PlayerManager.ApplyPlayerStates(currentState.PlayerStates);
        }

        /// <summary>
        /// SendStateAck
        ///
        /// Acknowledge a game state being received
        /// </summary>
        /// <param name="sequence">State sequence number</param>
        private void SendStateAck(uint sequence)
        {
            NetOutgoingMessage message = NetworkManager.CreateMessage();
            message.Write((byte)NetMessages.StateAck);
            message.Write(sequence);
            NetworkManager.ClientSendMessage(message, NetDeliveryMethod.Unreliable);
        }

        public void FormResize()
        {
            CluwneLib.ScreenViewportSize =
                new Vector2u(CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y);

            UserInterfaceManager.ResizeComponents();
            ResetRendertargets();
            IoCManager.Resolve<ILightManager>().RecalculateLights();
            RecalculateScene();
        }

        #endregion State

        private void OnPlayerMove(object sender, VectorEventArgs args)
        {
            //Recalculate scene batches for drawing.
            RecalculateScene();
        }

        public void OnTileChanged(TileRef tileRef, Tile oldTile)
        {
            IoCManager.Resolve<ILightManager>().RecalculateLightsInView(new FloatRect(tileRef.X, tileRef.Y, 1, 1));
            // Recalculate the scene batches.
            RecalculateScene();
        }

        #endregion Event Handlers

        #region Lighting in order of call

        /**
         *  Calculate lights In player view
         *  Render Lights in view to lightmap  > Screenshadows
         *
         *
         **/

        private void CalculateAllLights()
        {
            foreach
            (ILight l in IoCManager.Resolve<ILightManager>().GetLights().Where(l => l.LightArea.Calculated == false))
            {
                CalculateLightArea(l);
            }
        }

        /// <summary>
        /// Renders a set of lights into a single lightmap.
        /// If a light hasn't been prerendered yet, it renders that light.
        /// </summary>
        /// <param name="lights">Array of lights</param>
        private void RenderLightsIntoMap(IEnumerable<ILight> lights)
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
            screenShadows.Clear(Color.Black);

            RenderImage source = screenShadows;
            source.Clear(Color.Black);

            RenderImage desto = shadowIntermediate;
            RenderImage copy = null;

            Lightmap.setAsCurrentShader();

            var lightTextures = new List<Texture>();
            var colors = new List<Vector4f>();
            var positions = new List<Vector4f>();

            //Step 3 - Blend all the lights!
            foreach (ILight Light in lights)
            {
                //Skip off or broken lights (TODO code broken light states)
                if (Light.LightState != LightState.On)
                    continue;

                // LIGHT BLEND STAGE 1 - SIZING -- copys the light texture to a full screen rendertarget
                var area = (LightArea)Light.LightArea;

                //Set the drawing position.
                Vector2f blitPos = CluwneLib.WorldToScreen(area.LightPosition) - area.LightAreaSize * 0.5f;

                //Set shader parameters
                var LightPositionData = new Vector4f(blitPos.X / screenShadows.Width,
                                                    blitPos.Y / screenShadows.Height,
                                                    (float)screenShadows.Width / area.RenderTarget.Width,
                                                    (float)screenShadows.Height / area.RenderTarget.Height);
                lightTextures.Add(area.RenderTarget.Texture);
                colors.Add(Light.GetColorVec());
                positions.Add(LightPositionData);
            }
            int i = 0;
            int num_lights = 6;
            bool draw = false;
            bool fill = false;
            Texture black = IoCManager.Resolve<IResourceCache>().GetSprite("black5x5").Texture;
            var r_img = new Texture[num_lights];
            var r_col = new Vector4f[num_lights];
            var r_pos = new Vector4f[num_lights];
            do
            {
                if (fill)
                {
                    for (int j = i; j < num_lights; j++)
                    {
                        r_img[j] = black;
                        r_col[j] = Vector4f.Zero;
                        r_pos[j] = new Vector4f(0, 0, 1, 1);
                    }
                    i = num_lights;
                    draw = true;
                    fill = false;
                }
                if (draw)
                {
                    desto.BeginDrawing();

                    Lightmap.SetParameter("LightPosData", r_pos);
                    Lightmap.SetParameter("Colors", r_col);
                    Lightmap.SetParameter("light0", r_img[0]);
                    Lightmap.SetParameter("light1", r_img[1]);
                    Lightmap.SetParameter("light2", r_img[2]);
                    Lightmap.SetParameter("light3", r_img[3]);
                    Lightmap.SetParameter("light4", r_img[4]);
                    Lightmap.SetParameter("light5", r_img[5]);
                    Lightmap.SetParameter("sceneTexture", source);

                    // Blit the shadow image on top of the screen
                    source.Blit(0, 0, source.Width, source.Height, BlitterSizeMode.Crop);

                    desto.EndDrawing();

                    //Swap rendertargets to set up for the next light
                    copy = source;
                    source = desto;
                    desto = copy;
                    i = 0;

                    draw = false;
                    fill = false;
                    r_img = new Texture[num_lights];
                    r_col = new Vector4f[num_lights];
                    r_pos = new Vector4f[num_lights];
                }
                if (lightTextures.Count > 0)
                {
                    r_img[i] = lightTextures[0];
                    lightTextures.RemoveAt(0);

                    r_col[i] = colors[0];
                    colors.RemoveAt(0);

                    r_pos[i] = positions[0];
                    positions.RemoveAt(0);

                    i++;
                }
                if (i == num_lights)
                    //if I is equal to 6 draw
                    draw = true;
                if (i > 0 && i < num_lights && lightTextures.Count == 0)
                    // If all light textures in lightTextures have been processed, fill = true
                    fill = true;
            } while (lightTextures.Count > 0 || draw || fill);

            Lightmap.ResetCurrentShader();

            if (source != screenShadows)
            {
                screenShadows.BeginDrawing();
                source.Blit(0, 0, source.Width, source.Height);
                screenShadows.EndDrawing();
            }

            IResourceCache resCache = IoCManager.Resolve<IResourceCache>();
            Dictionary<Texture, string> tmp = resCache.TextureToKey;
            if (!tmp.ContainsKey(screenShadows.Texture)) { return; } //if it doesn't exist, something's fucked
            string textureKey = tmp[screenShadows.Texture];
            Image texunflipx = Graphics.TexHelpers.TextureCache.Textures[textureKey].Image;
            texunflipx.FlipVertically();
            screenShadows.Texture.Update(texunflipx);
        }

        private void CalculateSceneBatches(FloatRect vision)
        {
            if (!_recalculateScene)
                return;

            // Render the player sightline occluder
            RenderPlayerVisionMap();

            //Blur the player vision map
            BlurPlayerVision();

            _decalBatch.BeginDrawing();
            _wallTopsBatch.BeginDrawing();
            _floorBatch.BeginDrawing();
            _wallBatch.BeginDrawing();
            _gasBatch.BeginDrawing();

            DrawTiles(vision);

            _floorBatch.EndDrawing();
            _decalBatch.EndDrawing();
            _wallTopsBatch.EndDrawing();
            _gasBatch.EndDrawing();
            _wallBatch.EndDrawing();

            _recalculateScene = false;
            _redrawTiles = true;
            _redrawOverlay = true;
        }

        private void RenderPlayerVisionMap()
        {
            if (bFullVision)
            {
                playerOcclusionTarget.Clear(new SFML.Graphics.Color(211, 211, 211));
                return;
            }
            if (bPlayerVision)
            {
                // I think this should be transparent? Maybe it should be black for the player occlusion...
                // I don't remember. --volundr
                playerOcclusionTarget.Clear(Color.Black);
                playerVision.Move(PlayerManager.ControlledEntity.GetComponent<ITransformComponent>().Position.Convert());

                LightArea area = GetLightArea(RadiusToShadowMapSize(playerVision.Radius));
                area.LightPosition = playerVision.Position; // Set the light position

                TileRef TileReference = MapManager.GetTileRef(playerVision.Position);

                if (TileReference.Tile.TileDef.IsOpaque)
                {
                    area.LightPosition = new Vector2f(area.LightPosition.X, TileReference.Y + MapManager.TileSize + 1);
                }

                area.BeginDrawingShadowCasters(); // Start drawing to the light rendertarget
                DrawWallsRelativeToLight(area); // Draw all shadowcasting stuff here in black
                area.EndDrawingShadowCasters(); // End drawing to the light rendertarget

                Vector2f blitPos = CluwneLib.WorldToScreen(area.LightPosition) - area.LightAreaSize * 0.5f;
                var tmpBlitPos = CluwneLib.WorldToScreen(area.LightPosition) -
                                 new Vector2f(area.RenderTarget.Width, area.RenderTarget.Height) * 0.5f;

                if (debugWallOccluders)
                {
                    _occluderDebugTarget.BeginDrawing();
                    _occluderDebugTarget.Clear(Color.White);
                    area.RenderTarget.Blit((int)tmpBlitPos.X, (int)tmpBlitPos.Y, area.RenderTarget.Width, area.RenderTarget.Height,
                        Color.White, BlitterSizeMode.Crop);
                    _occluderDebugTarget.EndDrawing();
                }

                shadowMapResolver.ResolveShadows(area, false, IoCManager.Resolve<IResourceCache>().GetSprite("whitemask").Texture); // Calc shadows

                if (debugPlayerShadowMap)
                {
                    _occluderDebugTarget.BeginDrawing();
                    _occluderDebugTarget.Clear(Color.White);
                    area.RenderTarget.Blit((int)tmpBlitPos.X, (int)tmpBlitPos.Y, area.RenderTarget.Width, area.RenderTarget.Height, Color.White, BlitterSizeMode.Crop);
                    _occluderDebugTarget.EndDrawing();
                }

                playerOcclusionTarget.BeginDrawing(); // Set to shadow rendertarget

                //area.renderTarget.SourceBlend = AlphaBlendOperation.One;
                //area.renderTarget.DestinationBlend = AlphaBlendOperation.Zero;
                area.RenderTarget.BlendSettings.ColorSrcFactor = BlendMode.Factor.One;
                area.RenderTarget.BlendSettings.ColorDstFactor = BlendMode.Factor.Zero;

                area.RenderTarget.Blit((int)blitPos.X, (int)blitPos.Y, area.RenderTarget.Width, area.RenderTarget.Height, Color.White, BlitterSizeMode.Crop);

                //area.renderTarget.SourceBlend = AlphaBlendOperation.SourceAlpha; //reset blend mode
                //area.renderTarget.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha; //reset blend mode
                area.RenderTarget.BlendSettings.ColorDstFactor = BlendMode.Factor.SrcAlpha;
                area.RenderTarget.BlendSettings.ColorDstFactor = BlendMode.Factor.OneMinusSrcAlpha;

                playerOcclusionTarget.EndDrawing();

                //Debug.DebugRendertarget(playerOcclusionTarget);
            }
            else
            {
                playerOcclusionTarget.Clear(Color.Black);
            }
        }

        // Draws all walls in the area around the light relative to it, and in black (test code, not pretty)
        private void DrawWallsRelativeToLight(ILightArea area)
        {
            Vector2f lightAreaSize = CluwneLib.PixelToTile(area.LightAreaSize) / 2;
            var lightArea = new FloatRect(area.LightPosition - lightAreaSize, CluwneLib.PixelToTile(area.LightAreaSize));

            var tiles = MapManager.GetWallsIntersecting(lightArea);

            foreach (TileRef t in tiles)
            {
                Vector2f pos = area.ToRelativePosition(CluwneLib.WorldToScreen(new Vector2f(t.X, t.Y)));
                t.Tile.TileDef.RenderPos(pos.X, pos.Y);
            }
        }

        private void BlurPlayerVision()
        {
            _gaussianBlur.SetRadius(11);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Vector2f(playerOcclusionTarget.Width, playerOcclusionTarget.Height));
            _gaussianBlur.PerformGaussianBlur(playerOcclusionTarget);
        }

        /// <summary>
        /// Copys all tile sprites into batches.
        /// </summary>
        private void DrawTiles(FloatRect vision)
        {
            var tiles = MapManager.GetTilesIntersecting(vision, false);
            var walls = new List<TileRef>();

            foreach (TileRef TileReference in tiles)
            {
                var Tile = TileReference.Tile;
                var TileType = Tile.TileDef;

                //t.RenderGas(WindowOrigin.X, WindowOrigin.Y, tilespacing, _gasBatch);
                if (TileType.IsWall)
                    walls.Add(TileReference);
                else
                {
                    var point = CluwneLib.WorldToScreen(new Vector2f(TileReference.X, TileReference.Y));
                    TileType.Render(point.X, point.Y, _floorBatch);
                    TileType.RenderGas(point.X, point.Y, MapManager.TileSize, _gasBatch);
                }
            }

            walls.Sort((t1, t2) => t1.Y - t2.Y);

            foreach (TileRef tr in walls)
            {
                var t = tr.Tile;
                var td = t.TileDef;

                var point = CluwneLib.WorldToScreen(new Vector2f(tr.X, tr.Y));
                td.Render(point.X, point.Y, _wallBatch);
                td.RenderTop(point.X, point.Y, _wallTopsBatch);
            }
        }

        /// <summary>
        /// Render the renderables
        /// </summary>
        /// <param name="frametime">time since the last frame was rendered.</param>
        private void RenderComponents(float frameTime, FloatRect viewPort)
        {
            IEnumerable<IComponent> components = _componentManager.GetComponents<ISpriteRenderableComponent>()
                                          .Cast<IComponent>()
                                          .Union(_componentManager.GetComponents<ParticleSystemComponent>());

            IEnumerable<IRenderableComponent> floorRenderables = from IRenderableComponent c in components
                                                                 orderby c.Bottom ascending, c.DrawDepth ascending
                                                                 where c.DrawDepth < DrawDepth.MobBase
                                                                 select c;

            RenderList(new Vector2f(viewPort.Left, viewPort.Top), new Vector2f(viewPort.Right(), viewPort.Bottom()),
                       floorRenderables);

            IEnumerable<IRenderableComponent> largeRenderables = from IRenderableComponent c in components
                                                                 orderby c.Bottom ascending
                                                                 where c.DrawDepth >= DrawDepth.MobBase &&
                                                                       c.DrawDepth < DrawDepth.WallTops
                                                                 select c;

            RenderList(new Vector2f(viewPort.Left, viewPort.Top), new Vector2f(viewPort.Right(), viewPort.Bottom()),
                       largeRenderables);

            IEnumerable<IRenderableComponent> ceilingRenderables = from IRenderableComponent c in components
                                                                   orderby c.Bottom ascending, c.DrawDepth ascending
                                                                   where c.DrawDepth >= DrawDepth.WallTops
                                                                   select c;

            RenderList(new Vector2f(viewPort.Left, viewPort.Top), new Vector2f(viewPort.Right(), viewPort.Bottom()),
                       ceilingRenderables);
        }

        private void LightScene()
        {
            //Blur the light/shadow map
            BlurShadowMap();

            //Render the scene and lights together to compose the lit scene

            _composedSceneTarget.BeginDrawing();
            _composedSceneTarget.Clear(Color.Black);
            LightblendTechnique["FinalLightBlend"].setAsCurrentShader();
            Sprite outofview = IoCManager.Resolve<IResourceCache>().GetSprite("outofview");
            float texratiox = (float)CluwneLib.CurrentClippingViewport.Width / outofview.Texture.Size.X;
            float texratioy = (float)CluwneLib.CurrentClippingViewport.Height / outofview.Texture.Size.Y;
            var maskProps = new Vector4f(texratiox, texratioy, 0, 0);

            LightblendTechnique["FinalLightBlend"].SetParameter("PlayerViewTexture", playerOcclusionTarget);
            LightblendTechnique["FinalLightBlend"].SetParameter("OutOfViewTexture", outofview.Texture);
            LightblendTechnique["FinalLightBlend"].SetParameter("MaskProps", maskProps);
            LightblendTechnique["FinalLightBlend"].SetParameter("LightTexture", screenShadows);
            LightblendTechnique["FinalLightBlend"].SetParameter("SceneTexture", _sceneTarget);
            LightblendTechnique["FinalLightBlend"].SetParameter("AmbientLight", new Vector4f(.05f, .05f, 0.05f, 1));

            // Blit the shadow image on top of the screen
            screenShadows.Blit(0, 0, screenShadows.Width, screenShadows.Height, Color.White, BlitterSizeMode.Crop);

            LightblendTechnique["FinalLightBlend"].ResetCurrentShader();
            _composedSceneTarget.EndDrawing();

            //  Debug.DebugRendertarget(_composedSceneTarget);

            playerOcclusionTarget.ResetCurrentRenderTarget(); // set the rendertarget back to screen
            playerOcclusionTarget.Blit(0, 0, screenShadows.Width, screenShadows.Height, Color.White, BlitterSizeMode.Crop); //draw playervision again
            PlayerPostProcess();

            //redraw composed scene
            _composedSceneTarget.Blit(0, 0, (uint)CluwneLib.Screen.Size.X, (uint)CluwneLib.Screen.Size.Y, Color.White, BlitterSizeMode.Crop);

            //old
            //   screenShadows.Blit(0, 0, _tilesTarget.Width, _tilesTarget.Height, Color.White, BlitterSizeMode.Crop);
            //   playerOcclusionTarget.Blit(0, 0, _tilesTarget.Width, _tilesTarget.Height, Color.White, BlitterSizeMode.Crop);
        }

        private void BlurShadowMap()
        {
            _gaussianBlur.SetRadius(11);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Vector2f(screenShadows.Width, screenShadows.Height));
            _gaussianBlur.PerformGaussianBlur(screenShadows);
        }

        private void PlayerPostProcess()
        {
            PlayerManager.ApplyEffects(_composedSceneTarget);
        }

        #endregion Lighting in order of call

        #region Helper methods

        private void RenderList(Vector2f topleft, Vector2f bottomright, IEnumerable<IRenderableComponent> renderables)
        {
            foreach (IRenderableComponent component in renderables)
            {
                if (component is SpriteComponent)
                {
                    //Slaved components are drawn by their master
                    var c = component as SpriteComponent;
                    if (c.IsSlaved())
                        continue;
                }
                component.Render(topleft, bottomright);
            }
        }

        private void CalculateLightArea(ILight light)
        {
            ILightArea area = light.LightArea;
            if (area.Calculated)
                return;
            area.LightPosition = light.Position; //mousePosWorld; // Set the light position
            TileRef t = MapManager.GetTileRef(light.Position);
            if (t.Tile.IsSpace)
                return;
            if (t.Tile.TileDef.IsOpaque)
            {
                area.LightPosition = new Vector2f(area.LightPosition.X,
                                                  t.Y +
                                                  MapManager.TileSize + 1);
            }
            area.BeginDrawingShadowCasters(); // Start drawing to the light rendertarget
            DrawWallsRelativeToLight(area); // Draw all shadowcasting stuff here in black
            area.EndDrawingShadowCasters(); // End drawing to the light rendertarget
            shadowMapResolver.ResolveShadows((LightArea)area, true); // Calc shadows
            area.Calculated = true;
        }

        private ShadowmapSize RadiusToShadowMapSize(int Radius)
        {
            switch (Radius)
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

        private void RecalculateScene()
        {
            _recalculateScene = true;
        }

        private void ResetRendertargets()
        {
            foreach (var rt in _cleanupList)
                rt.Dispose();
            foreach (var sp in _cleanupSpriteList)
                sp.Dispose();

            InitializeRenderTargets();
            InitalizeLighting();
        }

        private void ToggleOccluderDebug()
        {
            debugWallOccluders = !debugWallOccluders;
        }

        #endregion Helper methods

        #region Nested type: ClickData

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

        #endregion Nested type: ClickData
    }
}
