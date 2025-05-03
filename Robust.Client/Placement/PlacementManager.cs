using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Log;
using Direction = Robust.Shared.Maths.Direction;
using Robust.Shared.Map.Components;

namespace Robust.Client.Placement
{
    public sealed partial class PlacementManager : IPlacementManager, IDisposable, IEntityEventSubscriber
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IClientNetManager _networkManager = default!;
        [Dependency] internal readonly IPlayerManager PlayerManager = default!;
        [Dependency] internal readonly IResourceCache ResourceCache = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] internal readonly IMapManager MapManager = default!;
        [Dependency] private readonly IGameTiming _time = default!;
        [Dependency] internal readonly IEyeManager EyeManager = default!;
        [Dependency] internal readonly IInputManager InputManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] internal readonly IEntityManager EntityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] internal readonly IClyde Clyde = default!;

        private ISawmill _sawmill = default!;

        private SharedMapSystem Maps => EntityManager.System<SharedMapSystem>();
        private SharedTransformSystem XformSystem => EntityManager.System<SharedTransformSystem>();

        /// <summary>
        ///     How long before a pending tile change is dropped.
        /// </summary>
        private static readonly TimeSpan PendingTileTimeout = TimeSpan.FromSeconds(2.0);

        /// <summary>
        /// Dictionary of all placement mode types
        /// </summary>
        private readonly Dictionary<string, Type> _modeDictionary = new();
        private readonly List<Tuple<EntityCoordinates, TimeSpan>> _pendingTileChanges = new();

        /// <summary>
        /// Tells this system to try to handle placement of an entity during the next frame
        /// </summary>
        private bool _placenextframe;

        // Massive hack to avoid creating a billion grids for now.
        private bool _gridFrameBuffer;

        /// <summary>
        /// Allows various types of placement as singular, line, or grid placement where placement mode allows this type of placement
        /// </summary>
        public PlacementTypes PlacementType { get; set; }

        /// <summary>
        /// Holds the anchor that we can try to spawn in a line or a grid from
        /// </summary>
        public EntityCoordinates StartPoint { get; set; }

        /// <summary>
        /// Whether the placement manager is currently in a mode where it accepts actions
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            private set
            {
                _isActive = value;

                if (CurrentPermission?.UseEditorContext is false)
                    return;

                SwitchEditorContext(value);
            }
        }

        /// <summary>
        /// Determines whether we are using the mode to delete an entity on click
        /// </summary>
        public bool Eraser { get; private set; }

        public bool Replacement { get; set; } = true;

        /// <summary>
        /// Holds the selection rectangle for the eraser
        /// </summary>
        public Box2? EraserRect { get; set; }

        /// <summary>
        /// Drawing shader for drawing without being affected by lighting
        /// </summary>
        private ShaderInstance? _drawingShader { get; set; }

        /// <summary>
        /// The entity for placement overlay.
        /// Colour of this gets swapped around in PlacementMode.
        /// This entity needs to stay in nullspace.
        /// </summary>
        public EntityUid? CurrentPlacementOverlayEntity { get; set; }

        /// <summary>
        /// A BAD way to explicitly control the icons used!!!
        /// Need to fix Content for this
        /// </summary>
        public List<IDirectionalTextureProvider>? CurrentTextures {
            set {
                PreparePlacementTexList(value, !Hijack?.CanRotate ?? value != null, null);
            }
        }

        /// <summary>
        /// Which of the placement orientations we are trying to place with
        /// </summary>
        public PlacementMode? CurrentMode { get; set; }

        public PlacementInformation? CurrentPermission { get; set; }

        public PlacementHijack? Hijack { get; private set; }

        private EntityPrototype? _currentPrototype;

        /// <summary>
        /// The prototype of the entity we are going to spawn on click
        /// </summary>
        public EntityPrototype? CurrentPrototype
        {
            get => _currentPrototype;
            set
            {
                _currentPrototype = value;

                if (value != null)
                {
                    PlacementOffset = value.PlacementOffset;
                }

                _colliderAABB = new Box2(0f, 0f, 0f, 0f);
            }
        }

        public Vector2i PlacementOffset { get; set; }


        private Box2 _colliderAABB = new(0f, 0f, 0f, 0f);

        /// <summary>
        /// The box which certain placement modes collision checks will be done against
        /// </summary>
        public Box2 ColliderAABB
        {
            get => _colliderAABB;
            set => _colliderAABB = value;
        }

        private Direction _direction = Direction.South;

        /// <inheritdoc />
        public Direction Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                DirectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        public event EventHandler? DirectionChanged;

        private PlacementOverlay _drawOverlay = default!;
        private bool _isActive;

        public void Initialize()
        {
            _drawingShader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();
            _sawmill = _logManager.GetSawmill("placement");

            _networkManager.RegisterNetMessage<MsgPlacement>(HandlePlacementMessage);

            _modeDictionary.Clear();
            foreach (var type in _reflectionManager.GetAllChildren<PlacementMode>())
            {
                _modeDictionary.Add(type.Name, type);
            }

            EntityManager.EventBus.SubscribeEvent<TileChangedEvent>(EventSource.Local, this, HandleTileChanged);

            _drawOverlay = new PlacementOverlay(this);
            _overlayManager.AddOverlay(_drawOverlay);

            // a bit ugly, oh well
            _baseClient.PlayerJoinedServer += (sender, args) => SetupInput();
            _baseClient.PlayerLeaveServer += (sender, args) => TearDownInput();
        }

        private void SetupInput()
        {
            CommandBinds.Builder
                .Bind(EngineKeyFunctions.EditorLinePlace, InputCmdHandler.FromDelegate(
                    session =>
                    {
                        if (IsActive && !Eraser) ActivateLineMode();
                    }))
                .Bind(EngineKeyFunctions.EditorGridPlace, InputCmdHandler.FromDelegate(
                    session =>
                    {
                        if (IsActive)
                        {
                            if (Eraser)
                            {
                                EraseRectMode();
                            }
                            else
                            {
                                ActivateGridMode();
                            }
                        }
                    }))
                .Bind(EngineKeyFunctions.EditorPlaceObject, new PointerStateInputCmdHandler(
                    (session, netCoords, nent) =>
                    {
                        if (!IsActive)
                            return false;

                        if (EraserRect.HasValue)
                        {
                            HandleRectDeletion(StartPoint, EraserRect.Value);
                            EraserRect = null;
                            return true;
                        }

                        if (Eraser)
                        {
                            if (HandleDeletion(netCoords))
                                return true;

                            if (nent == EntityUid.Invalid)
                            {
                                return false;
                            }

                            HandleDeletion(nent);
                        }
                        else
                        {
                            _placenextframe = true;
                        }

                        return true;
                    },
                    (session, coords, uid) =>
                    {
                        if (!IsActive || Eraser || !_placenextframe)
                            return false;

                        //Places objects for non-tile entities
                        if (!CurrentPermission!.IsTile)
                            HandlePlacement();

                        _gridFrameBuffer = false;
                        _placenextframe = false;
                        return true;
                    }, true))
                .Bind(EngineKeyFunctions.EditorRotateObject, InputCmdHandler.FromDelegate(
                    session =>
                    {
                        if (IsActive && !Eraser) Rotate();
                    }))
                .Bind(EngineKeyFunctions.EditorCancelPlace, InputCmdHandler.FromDelegate(
                    session =>
                    {
                        if (!IsActive)
                            return;
                        if (DeactivateSpecialPlacement())
                            return;
                        Clear();
                    }, outsidePrediction: true))
                .Register<PlacementManager>();

            PlayerManager.LocalPlayerDetached += OnDetached;
        }

        private void TearDownInput()
        {
            CommandBinds.Unregister<PlacementManager>();
            PlayerManager.LocalPlayerDetached -= OnDetached;
        }

        private void OnDetached(EntityUid obj)
        {
            Clear();
        }

        private void SwitchEditorContext(bool enabled)
        {
            if (enabled)
            {
                InputManager.Contexts.SetActiveContext("editor");
            }
            else
            {
                _entitySystemManager.GetEntitySystem<InputSystem>().SetEntityContextActive();
            }
        }

        public void Dispose()
        {
            _drawOverlay?.Dispose();
        }

        private void HandlePlacementMessage(MsgPlacement msg)
        {
            switch (msg.PlaceType)
            {
                case PlacementManagerMessage.StartPlacement:
                    HandleStartPlacement(msg);
                    break;
                case PlacementManagerMessage.CancelPlacement:
                    Clear();
                    break;
            }
        }

        private void HandleTileChanged(ref TileChangedEvent args)
        {
            var coords = Maps.GridTileToLocal(
                args.NewTile.GridUid,
                EntityManager.GetComponent<MapGridComponent>(args.NewTile.GridUid),
                args.NewTile.GridIndices);

            _pendingTileChanges.RemoveAll(c => c.Item1 == coords);
        }

        /// <inheritdoc />
        public event EventHandler? PlacementChanged;

        public void Clear()
        {
            ClearWithoutDeactivation();
            IsActive = false;
        }

        private void ClearWithoutDeactivation()
        {
            PlacementChanged?.Invoke(this, EventArgs.Empty);
            Hijack = null;
            EnsureNoPlacementOverlayEntity();
            CurrentPrototype = null;
            CurrentPermission = null;
            CurrentMode = null;
            DeactivateSpecialPlacement();
            _placenextframe = false;
            Eraser = false;
            EraserRect = null;
            PlacementOffset = Vector2i.Zero;
        }

        public void Rotate()
        {
            if (Hijack != null && !Hijack.CanRotate)
                return;

            switch (Direction)
            {
                case Direction.North:
                    Direction = Direction.East;
                    break;
                case Direction.East:
                    Direction = Direction.South;
                    break;
                case Direction.South:
                    Direction = Direction.West;
                    break;
                case Direction.West:
                    Direction = Direction.North;
                    break;
            }
        }

        public void HandlePlacement()
        {
            if (!IsActive || Eraser)
                return;

            switch (PlacementType)
            {
                case PlacementTypes.None:
                    RequestPlacement(CurrentMode!.MouseCoords);
                    break;
                case PlacementTypes.Line:
                    foreach (var coordinate in CurrentMode!.LineCoordinates())
                    {
                        RequestPlacement(coordinate);
                    }

                    DeactivateSpecialPlacement();
                    break;
                case PlacementTypes.Grid:
                    _gridFrameBuffer = true;
                    foreach (var coordinate in CurrentMode!.GridCoordinates())
                    {
                        RequestPlacement(coordinate);
                    }

                    DeactivateSpecialPlacement();
                    break;
            }
        }

        public bool HandleDeletion(EntityCoordinates coordinates)
        {
            if (!IsActive || !Eraser) return false;
            if (Hijack != null)
                return Hijack.HijackDeletion(coordinates);

            return false;
        }

        public void HandleDeletion(EntityUid entity)
        {
            if (!IsActive || !Eraser) return;
            if (Hijack != null && Hijack.HijackDeletion(entity)) return;

            var msg = new MsgPlacement();
            msg.PlaceType = PlacementManagerMessage.RequestEntRemove;
            msg.EntityUid = EntityManager.GetNetEntity(entity);
            _networkManager.ClientSendMessage(msg);
        }

        public void HandleRectDeletion(EntityCoordinates start, Box2 rect)
        {
            var msg = new MsgPlacement();
            msg.PlaceType = PlacementManagerMessage.RequestRectRemove;
            msg.NetCoordinates = new NetCoordinates(EntityManager.GetNetEntity(StartPoint.EntityId), rect.BottomLeft);
            msg.RectSize = rect.Size;
            _networkManager.ClientSendMessage(msg);
        }

        public void ToggleEraser()
        {
            if (!Eraser && !IsActive)
            {
                IsActive = true;
                Eraser = true;
            }
            else Clear();
        }

        public void ToggleEraserHijacked(PlacementHijack hijack)
        {
            if (!Eraser && !IsActive)
            {
                IsActive = true;
                Eraser = true;
                Hijack = hijack;
            }
            else Clear();
        }

        public void BeginPlacing(PlacementInformation info, PlacementHijack? hijack = null)
        {
            BeginHijackedPlacing(info, hijack);
        }

        public void BeginHijackedPlacing(PlacementInformation info, PlacementHijack? hijack = null)
        {
            ClearWithoutDeactivation();

            CurrentPermission = info;

            if (!_modeDictionary.TryFirstOrNull(pair => pair.Key.Equals(CurrentPermission.PlacementOption), out KeyValuePair<string, Type>? placeMode))
            {
                _sawmill.Log(LogLevel.Warning, $"Invalid placement mode `{CurrentPermission.PlacementOption}`");
                Clear();
                return;
            }

            CurrentMode = (PlacementMode) Activator.CreateInstance(placeMode.Value.Value, this)!;

            if (hijack != null)
            {
                Hijack = hijack;
                Hijack.StartHijack(this);
                IsActive = true;
                return;
            }

            if (info.IsTile)
                PreparePlacementTile();
            else
                PreparePlacement(info.EntityType!);
        }

        private bool CurrentMousePosition(out ScreenCoordinates coordinates)
        {
            // Try to get current map.
            var map = MapId.Nullspace;
            if (EntityManager.TryGetComponent(PlayerManager.LocalEntity, out TransformComponent? xform))
            {
                map = xform.MapID;
            }

            if (map == MapId.Nullspace || CurrentPermission == null || CurrentMode == null)
            {
                coordinates = default;
                return false;
            }

            coordinates = InputManager.MouseScreenPosition;
            return true;
        }

        private bool CurrentEraserMouseCoordinates(out EntityCoordinates coordinates)
        {
            var ent = PlayerManager.LocalEntity ?? EntityUid.Invalid;
            if (ent == EntityUid.Invalid)
            {
                coordinates = new EntityCoordinates();
                return false;
            }
            else
            {
                var mousePosition = EyeManager.PixelToMap(InputManager.MouseScreenPosition);
                var map = EntityManager.GetComponent<TransformComponent>(ent).MapID;
                if (map == MapId.Nullspace || !Eraser || mousePosition.MapId == MapId.Nullspace)
                {
                    coordinates = new EntityCoordinates();
                    return false;
                }
                coordinates = XformSystem.ToCoordinates(mousePosition);
                return true;
            }
        }

        /// <inheritdoc />
        public void FrameUpdate(FrameEventArgs e)
        {
            if (!CurrentMousePosition(out var mouseScreen))
            {
                if (EraserRect.HasValue)
                {
                    if (!CurrentEraserMouseCoordinates(out EntityCoordinates end))
                        return;
                    float b, l, t, r;
                    if (StartPoint.X < end.X)
                    {
                        l = StartPoint.X;
                        r = end.X;
                    }
                    else
                    {
                        l = end.X;
                        r = StartPoint.X;
                    }
                    if (StartPoint.Y < end.Y)
                    {
                        b = StartPoint.Y;
                        t = end.Y;
                    }
                    else
                    {
                        b = end.Y;
                        t = StartPoint.Y;
                    }
                    EraserRect = new Box2(l, b, r, t);
                }
                return;
            }

            CurrentMode!.AlignPlacementMode(mouseScreen);

            // purge old unapproved tile changes
            _pendingTileChanges.RemoveAll(c => c.Item2 < _time.RealTime);

            // continues tile placement but placement of entities only occurs on mouseUp
            if (_placenextframe && CurrentPermission!.IsTile && !_gridFrameBuffer)
            {
                HandlePlacement();
            }
        }

        private void ActivateLineMode()
        {
            if (!CurrentMode!.HasLineMode)
                return;

            if (!CurrentMousePosition(out var mouseScreen))
                return;

            CurrentMode.AlignPlacementMode(mouseScreen);
            StartPoint = CurrentMode.MouseCoords;
            PlacementType = PlacementTypes.Line;
        }

        private void ActivateGridMode()
        {
            if (!CurrentMode!.HasGridMode)
                return;

            if (!CurrentMousePosition(out var mouseScreen))
                return;

            CurrentMode.AlignPlacementMode(mouseScreen);
            StartPoint = CurrentMode.MouseCoords;
            PlacementType = PlacementTypes.Grid;
        }

        private void EraseRectMode()
        {
            if (!CurrentEraserMouseCoordinates(out EntityCoordinates coordinates))
                return;

            StartPoint = coordinates;
            EraserRect = new Box2(coordinates.Position, Vector2.Zero);
        }

        private bool DeactivateSpecialPlacement()
        {
            if (PlacementType == PlacementTypes.None)
                return false;

            PlacementType = PlacementTypes.None;
            return true;
        }

        private void Render(in OverlayDrawArgs args)
        {
            if (CurrentMode == null || !IsActive)
            {
                if (EraserRect.HasValue)
                {
                    args.WorldHandle.UseShader(_drawingShader);
                    args.WorldHandle.DrawRect(EraserRect.Value, new Color(255, 0, 0, 50));
                    args.WorldHandle.UseShader(null);
                }
                return;
            }

            CurrentMode.Render(args);

            if (CurrentPermission is not {Range: > 0} ||
                !CurrentMode.RangeRequired ||
                PlayerManager.LocalEntity is not {Valid: true} controlled)
                return;

            var worldPos = XformSystem.GetWorldPosition(controlled);

            args.WorldHandle.DrawCircle(worldPos, CurrentPermission.Range, new Color(1, 1, 1, 0.25f));
        }

        private void HandleStartPlacement(MsgPlacement msg)
        {
            CurrentPermission = new PlacementInformation
            {
                Range = msg.Range,
                IsTile = msg.IsTile,
            };

            CurrentPermission.EntityType = msg.ObjType; // tile or ent type
            CurrentPermission.PlacementOption = msg.AlignOption;

            BeginPlacing(CurrentPermission);
        }

        private void EnsureNoPlacementOverlayEntity()
        {
            if (CurrentPlacementOverlayEntity == null)
                return;

            if (!EntityManager.Deleted(CurrentPlacementOverlayEntity))
                EntityManager.DeleteEntity(CurrentPlacementOverlayEntity.Value);

            CurrentPlacementOverlayEntity = null;
        }

        private SpriteComponent SetupPlacementOverlayEntity()
        {
            EnsureNoPlacementOverlayEntity();
            CurrentPlacementOverlayEntity = EntityManager.SpawnEntity(null, MapCoordinates.Nullspace);
            return EntityManager.EnsureComponent<SpriteComponent>(CurrentPlacementOverlayEntity.Value);
        }

        private void PreparePlacement(string templateName)
        {
            EnsureNoPlacementOverlayEntity();

            var prototype = _prototypeManager.Index<EntityPrototype>(templateName);
            CurrentPrototype = prototype;
            IsActive = true;

            CurrentPlacementOverlayEntity = EntityManager.SpawnEntity(templateName, MapCoordinates.Nullspace);
            EntityManager.RunMapInit(
                CurrentPlacementOverlayEntity.Value,
                EntityManager.GetComponent<MetaDataComponent>(CurrentPlacementOverlayEntity.Value));
        }

        public void PreparePlacementSprite(SpriteComponent sprite)
        {
            var sc = SetupPlacementOverlayEntity();
            sc.CopyFrom(sprite);
        }

        public void PreparePlacementTexList(List<IDirectionalTextureProvider>? texs, bool noRot, EntityPrototype? prototype)
        {
            var sc = SetupPlacementOverlayEntity();
            if (texs != null)
            {
                // This one covers most cases (including Construction)
                foreach (var v in texs)
                {
                    if (v is RSI.State)
                    {
                        var st = (RSI.State) v;
                        sc.AddLayer(st.StateId, st.RSI);
                    }
                    else
                    {
                        // Fallback
                        sc.AddLayer(v.Default);
                    }
                }
            }
            else
            {
                sc.AddLayer(new ResPath("/Textures/Interface/tilebuildoverlay.png"));
            }
            sc.NoRotation = noRot;

            if (prototype != null && prototype.TryGetComponent<SpriteComponent>("Sprite", out var spriteComp))
            {
                sc.Scale = spriteComp.Scale;
            }

        }

        private void PreparePlacementTile()
        {
            var sc = SetupPlacementOverlayEntity();
            sc.AddLayer(new ResPath("/Textures/Interface/tilebuildoverlay.png"));

            IsActive = true;
        }

        private void RequestPlacement(EntityCoordinates coordinates)
        {
            if (CurrentPermission == null) return;
            if (!CurrentMode!.IsValidPosition(coordinates)) return;
            if (Hijack != null && Hijack.HijackPlacementRequest(coordinates)) return;

            if (CurrentPermission.IsTile)
            {
                var gridIdOpt = XformSystem.GetGrid(coordinates);
                // If we have actually placed something on a valid grid...
                if (gridIdOpt is { } gridId && gridId.IsValid())
                {
                    var grid = EntityManager.GetComponent<MapGridComponent>(gridId);

                    // no point changing the tile to the same thing.
                    if (Maps.GetTileRef(gridId, grid, coordinates).Tile.TypeId == CurrentPermission.TileType)
                        return;
                }

                foreach (var tileChange in _pendingTileChanges)
                {
                    // if change already pending, ignore it
                    if (tileChange.Item1 == coordinates)
                        return;
                }

                var tuple = new Tuple<EntityCoordinates, TimeSpan>(coordinates, _time.RealTime + PendingTileTimeout);
                _pendingTileChanges.Add(tuple);
            }

            var message = new MsgPlacement
            {
                PlaceType = PlacementManagerMessage.RequestPlacement,
                Align = CurrentMode.ModeName,
                IsTile = CurrentPermission.IsTile,
                Replacement = Replacement
            };

            if (CurrentPermission.IsTile)
                message.TileType = CurrentPermission.TileType;
            else
                message.EntityTemplateName = CurrentPermission.EntityType;

            // world x and y
            message.NetCoordinates = EntityManager.GetNetCoordinates(coordinates);

            message.DirRcv = Direction;

            _networkManager.ClientSendMessage(message);
        }

        public enum PlacementTypes : byte
        {
            None = 0,
            Line = 1,
            Grid = 2
        }
    }
}
