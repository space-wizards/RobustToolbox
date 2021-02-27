using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Network.Messages;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using Robust.Shared.Log;

namespace Robust.Client.Placement
{
    public partial class PlacementManager : IPlacementManager, IDisposable
    {
        [Dependency] public readonly IPhysicsManager PhysicsManager = default!;
        [Dependency] private readonly IClientNetManager NetworkManager = default!;
        [Dependency] public readonly IPlayerManager PlayerManager = default!;
        [Dependency] public readonly IResourceCache ResourceCache = default!;
        [Dependency] private readonly IReflectionManager ReflectionManager = default!;
        [Dependency] public readonly IMapManager MapManager = default!;
        [Dependency] private readonly IGameTiming _time = default!;
        [Dependency] public readonly IEyeManager eyeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] public readonly IEntityManager EntityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] public readonly IClyde _clyde = default!;

        /// <summary>
        ///     How long before a pending tile change is dropped.
        /// </summary>
        private static readonly TimeSpan _pendingTileTimeout = TimeSpan.FromSeconds(2.0);

        /// <summary>
        /// Dictionary of all placement mode types
        /// </summary>
        private readonly Dictionary<string, Type> _modeDictionary = new();
        private readonly List<Tuple<EntityCoordinates, TimeSpan>> _pendingTileChanges = new();

        /// <summary>
        /// Tells this system to try to handle placement of an entity during the next frame
        /// </summary>
        private bool _placenextframe;

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
                SwitchEditorContext(value);
            }
        }

        /// <summary>
        /// Determines whether we are using the mode to delete an entity on click
        /// </summary>
        public bool Eraser { get; private set; }

        /// <summary>
        /// Holds the selection rectangle for the eraser
        /// </summary>
        public Box2? EraserRect { get; set; }

        /// <summary>
        /// Drawing shader for drawing without being affected by lighting
        /// </summary>
        private ShaderInstance? _drawingShader { get; set; }

        /// <summary>
        /// The texture we use to show from our placement manager to represent the entity to place
        /// </summary>
        public List<IDirectionalTextureProvider>? CurrentTextures { get; set; }

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


                    /* todo paul
                     if (value.Components.TryGetValue("Physics", out var physicsComp)
                        && physicsComp is PhysicsComponent_AUTODATA physicsComponentAutodata
                        && physicsComponentAutodata._physShapes_field is { } shapes)
                    {
                        _colliderAABB = shapes.FirstOrDefault()?.CalculateLocalBounds(0f) ?? new Box2(0f, 0f, 0f, 0f);
                        return;
                    }*/
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

        /// <summary>
        /// The directional to spawn the entity in
        /// </summary>
        public Direction Direction { get; set; } = Direction.South;

        private PlacementOverlay _drawOverlay = default!;
        private bool _isActive;

        public void Initialize()
        {
            _drawingShader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();

            NetworkManager.RegisterNetMessage<MsgPlacement>(MsgPlacement.NAME, HandlePlacementMessage);

            _modeDictionary.Clear();
            foreach (var type in ReflectionManager.GetAllChildren<PlacementMode>())
            {
                _modeDictionary.Add(type.Name, type);
            }

            MapManager.TileChanged += HandleTileChanged;

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
                    (session, coords, uid) =>
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
                            if (HandleDeletion(coords))
                                return true;

                            if (uid == EntityUid.Invalid)
                            {
                                return false;
                            }

                            HandleDeletion(EntityManager.GetEntity(uid));
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

                        _placenextframe = false;
                        return true;
                    }))
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
                    }))
                .Register<PlacementManager>();

            var localPlayer = PlayerManager.LocalPlayer;
            localPlayer!.EntityAttached += OnEntityAttached;
        }

        private void TearDownInput()
        {
            CommandBinds.Unregister<PlacementManager>();

            if (PlayerManager.LocalPlayer != null)
            {
                PlayerManager.LocalPlayer.EntityAttached -= OnEntityAttached;
            }
        }

        private void OnEntityAttached(EntityAttachedEventArgs eventArgs)
        {
            // player attached to a new entity, basically disable the editor
            Clear();
        }

        private void SwitchEditorContext(bool enabled)
        {
            if (enabled)
            {
                _inputManager.Contexts.SetActiveContext("editor");
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

        private void HandleTileChanged(object? sender, TileChangedEventArgs args)
        {
            var coords = MapManager.GetGrid(args.NewTile.GridIndex).GridTileToLocal(args.NewTile.GridIndices);
            _pendingTileChanges.RemoveAll(c => c.Item1 == coords);
        }

        /// <inheritdoc />
        public event EventHandler? PlacementChanged;

        public void Clear()
        {
            PlacementChanged?.Invoke(this, EventArgs.Empty);
            Hijack = null;
            CurrentTextures = null;
            CurrentPrototype = null;
            CurrentPermission = null;
            CurrentMode = null;
            DeactivateSpecialPlacement();
            _placenextframe = false;
            IsActive = false;
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

            CurrentMode?.SetSprite();
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

        public void HandleDeletion(IEntity entity)
        {
            if (!IsActive || !Eraser) return;
            if (Hijack != null && Hijack.HijackDeletion(entity)) return;

            var msg = NetworkManager.CreateNetMessage<MsgPlacement>();
            msg.PlaceType = PlacementManagerMessage.RequestEntRemove;
            msg.EntityUid = entity.Uid;
            NetworkManager.ClientSendMessage(msg);
        }

        public void HandleRectDeletion(EntityCoordinates start, Box2 rect)
        {
            var msg = NetworkManager.CreateNetMessage<MsgPlacement>();
            msg.PlaceType = PlacementManagerMessage.RequestRectRemove;
            msg.EntityCoordinates = new EntityCoordinates(StartPoint.EntityId, rect.BottomLeft);
            msg.RectSize = rect.Size;
            NetworkManager.ClientSendMessage(msg);
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
            Clear();

            CurrentPermission = info;

            if (!_modeDictionary.Any(pair => pair.Key.Equals(CurrentPermission.PlacementOption)))
            {
                Clear();
                return;
            }

            var modeType = _modeDictionary.First(pair => pair.Key.Equals(CurrentPermission.PlacementOption)).Value;
            CurrentMode = (PlacementMode) Activator.CreateInstance(modeType, this)!;

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
            var ent = PlayerManager.LocalPlayer!.ControlledEntity;
            if (ent != null)
            {
                map = ent.Transform.MapID;
            }

            if (map == MapId.Nullspace || CurrentPermission == null || CurrentMode == null)
            {
                coordinates = new ScreenCoordinates(Vector2.Zero);
                return false;
            }

            coordinates = new ScreenCoordinates(_inputManager.MouseScreenPosition);
            return true;
        }

        private bool CurrentEraserMouseCoordinates(out EntityCoordinates coordinates)
        {
            var ent = PlayerManager.LocalPlayer?.ControlledEntity;
            if (ent == null)
            {
                coordinates = new EntityCoordinates();
                return false;
            }
            else
            {
                var map = ent.Transform.MapID;
                if (map == MapId.Nullspace || !Eraser)
                {
                    coordinates = new EntityCoordinates();
                    return false;
                }
                coordinates = EntityCoordinates.FromMap(ent.EntityManager, MapManager,
                    eyeManager.ScreenToMap(new ScreenCoordinates(_inputManager.MouseScreenPosition)));
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
            if (_placenextframe && CurrentPermission!.IsTile)
                HandlePlacement();
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

        private void Render(DrawingHandleWorld handle)
        {
            if (CurrentMode == null || !IsActive)
            {
                if (EraserRect.HasValue)
                {
                    handle.UseShader(_drawingShader);
                    handle.DrawRect(EraserRect.Value, new Color(255, 0, 0, 50));
                }
                return;
            }

            CurrentMode.Render(handle);

            if (CurrentPermission == null || CurrentPermission.Range <= 0 || !CurrentMode.RangeRequired
                || PlayerManager.LocalPlayer?.ControlledEntity == null)
                return;

            var worldPos = PlayerManager.LocalPlayer.ControlledEntity.Transform.WorldPosition;

            handle.DrawCircle(worldPos, CurrentPermission.Range, new Color(1, 1, 1, 0.25f));
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

        private void PreparePlacement(string templateName)
        {
            var prototype = _prototypeManager.Index<EntityPrototype>(templateName);

            CurrentTextures = SpriteComponent.GetPrototypeTextures(prototype, ResourceCache).ToList();
            CurrentPrototype = prototype;

            IsActive = true;
        }

        private void PreparePlacementTile()
        {
            CurrentTextures = new List<IDirectionalTextureProvider>
            {ResourceCache
                .GetResource<TextureResource>(new ResourcePath("/Textures/UserInterface/tilebuildoverlay.png")).Texture};

            IsActive = true;
        }

        private void RequestPlacement(EntityCoordinates coordinates)
        {
            if (CurrentPermission == null) return;
            if (!CurrentMode!.IsValidPosition(coordinates)) return;
            if (Hijack != null && Hijack.HijackPlacementRequest(coordinates)) return;

            if (CurrentPermission.IsTile)
            {
                var gridId = coordinates.GetGridId(EntityManager);
                // If we have actually placed something on a valid grid...
                if (gridId.IsValid())
                {
                    var grid = MapManager.GetGrid(gridId);

                    // no point changing the tile to the same thing.
                    if (grid.GetTileRef(coordinates).Tile.TypeId == CurrentPermission.TileType)
                        return;
                }

                foreach (var tileChange in _pendingTileChanges)
                {
                    // if change already pending, ignore it
                    if (tileChange.Item1 == coordinates)
                        return;
                }

                var tuple = new Tuple<EntityCoordinates, TimeSpan>(coordinates, _time.RealTime + _pendingTileTimeout);
                _pendingTileChanges.Add(tuple);
            }

            var message = NetworkManager.CreateNetMessage<MsgPlacement>();
            message.PlaceType = PlacementManagerMessage.RequestPlacement;

            message.Align = CurrentMode.ModeName;
            message.IsTile = CurrentPermission.IsTile;

            if (CurrentPermission.IsTile)
                message.TileType = CurrentPermission.TileType;
            else
                message.EntityTemplateName = CurrentPermission.EntityType;

            // world x and y
            message.EntityCoordinates = coordinates;

            message.DirRcv = Direction;

            NetworkManager.ClientSendMessage(message);
        }

        public enum PlacementTypes : byte
        {
            None = 0,
            Line = 1,
            Grid = 2
        }
    }
}
