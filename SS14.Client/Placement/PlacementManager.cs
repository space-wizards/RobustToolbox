using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.Input;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.ResourceManagement;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using SS14.Shared.Maths;
using SS14.Shared.Map;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Client.Utility;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Graphics;
using SS14.Client.GameObjects;
using SS14.Shared.Utility;
using SS14.Shared.Serialization;

namespace SS14.Client.Placement
{
    public class PlacementManager : IPlacementManager, IDisposable
    {
        [Dependency]
        public readonly ICollisionManager CollisionManager;
        [Dependency]
        private readonly IClientNetManager NetworkManager;
        [Dependency]
        public readonly IPlayerManager PlayerManager;
        [Dependency]
        public readonly IResourceCache ResourceCache;
        [Dependency]
        private readonly IReflectionManager ReflectionManager;
        [Dependency]
        private readonly IMapManager _mapMan;
        [Dependency]
        private readonly IGameTiming _time;
        [Dependency]
        public readonly IEyeManager eyeManager;
        [Dependency]
        public readonly ISceneTreeHolder sceneTree;
        [Dependency]
        readonly public IInputManager inputManager;
        [Dependency]
        private readonly IUserInterfaceManager _userInterfaceManager;
        [Dependency]
        private readonly IPrototypeManager _prototypeManager;
        [Dependency]
        private readonly ITileDefinitionManager _tileDefManager;

        /// <summary>
        ///     How long before a pending tile change is dropped.
        /// </summary>
        private static readonly TimeSpan _pendingTileTimeout = TimeSpan.FromSeconds(2.0);

        /// <summary>
        /// Dictionary of all placement mode types
        /// </summary>
        private readonly Dictionary<string, Type> _modeDictionary = new Dictionary<string, Type>();
        private readonly List<Tuple<GridLocalCoordinates, TimeSpan>> _pendingTileChanges = new List<Tuple<GridLocalCoordinates, TimeSpan>>();

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
        public GridLocalCoordinates StartPoint { get; set; }

        /// <summary>
        /// Whether the placement manager is currently in a mode where it accepts actions
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Determines whether we are using the mode to delete an entity on click
        /// </summary>
        public bool Eraser { get; private set; }

        /// <summary>
        /// The texture we use to show from our placement manager to represent the entity to place
        /// </summary>
        public IDirectionalTextureProvider CurrentBaseSprite { get; set; }

        /// <summary>
        /// Which of the placement orientations we are trying to place with
        /// </summary>
        public PlacementMode CurrentMode { get; set; }

        public PlacementInformation CurrentPermission { get; set; }

        private EntityPrototype _currentPrototype;

        /// <summary>
        /// The prototype of the entity we are going to spawn on click
        /// </summary>
        public EntityPrototype CurrentPrototype
        {
            get => _currentPrototype;
            set
            {
                _currentPrototype = value;
                //var name = BoundingBoxComponent.BoundingBoxName;
                if (value != null
                    && value.Components.ContainsKey("BoundingBox")
                    && value.Components.ContainsKey("Collidable"))
                {
                    var map = value.Components["BoundingBox"];
                    var serializer = new YamlObjectSerializer(map, reading: true);
                    serializer.DataField(ref _colliderAABB, "aabb", new Box2(0f, 0f, 0f, 0f));
                }
                else
                {
                    _colliderAABB = new Box2(0f, 0f, 0f, 0f);
                }
            }
        }

        private Box2 _colliderAABB = new Box2(0f, 0f, 0f, 0f);

        /// <summary>
        /// The box which certain placement modes collision checks will be done against
        /// </summary>
        public Box2 ColliderAABB => _colliderAABB;

        /// <summary>
        /// The directional to spawn the entity in
        /// </summary>
        public Direction Direction { get; set; } = Direction.South;

        public Godot.Node2D drawNode { get; set; }
        private GodotGlue.GodotSignalSubscriber0 drawNodeDrawSubscriber;

        public PlacementManager()
        {
            Clear();
        }

        public void Initialize()
        {
            NetworkManager.RegisterNetMessage<MsgPlacement>(MsgPlacement.NAME, HandlePlacementMessage);

            _modeDictionary.Clear();
            foreach (var type in ReflectionManager.GetAllChildren<PlacementMode>())
            {
                _modeDictionary.Add(type.Name, type);
            }

            _mapMan.TileChanged += HandleTileChanged;

            var UnshadedMaterial = new Godot.CanvasItemMaterial()
            {
                LightMode = Godot.CanvasItemMaterial.LightModeEnum.Unshaded
            };

            drawNode = new Godot.Node2D()
            {
                Name = "Placement Manager Sprite",
                ZIndex = 100,
                Material = UnshadedMaterial
            };
            sceneTree.WorldRoot.AddChild(drawNode);
            drawNodeDrawSubscriber = new GodotGlue.GodotSignalSubscriber0();
            drawNodeDrawSubscriber.Connect(drawNode, "draw");
            drawNodeDrawSubscriber.Signal += Render;
        }

        public void Dispose()
        {
            drawNodeDrawSubscriber.Disconnect(drawNode, "draw");
            drawNodeDrawSubscriber.Dispose();
            drawNode.QueueFree();
            drawNode.Dispose();
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

        private void HandleTileChanged(object sender, TileChangedEventArgs args)
        {
            var coords = args.NewTile.LocalPos;
            _pendingTileChanges.RemoveAll(c => c.Item1 == coords);
        }

        public event EventHandler PlacementCanceled;

        public void Clear()
        {
            CurrentBaseSprite = null;
            CurrentPrototype = null;
            CurrentPermission = null;
            CurrentMode = null;
            DeactivateSpecialPlacement();
            if (PlacementCanceled != null && IsActive && !Eraser) PlacementCanceled(this, null);
            _placenextframe = false;
            IsActive = false;
            Eraser = false;
            // Make it draw again to remove the drawn things.
            drawNode?.Update();
        }

        public void Rotate()
        {
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

            if (CurrentMode != null)
            {
                CurrentMode.SetSprite();
            }
        }

        public void HandlePlacement()
        {
            if (IsActive && !Eraser)
            {
                switch (PlacementType)
                {
                    case PlacementTypes.None:
                        RequestPlacement(CurrentMode.MouseCoords);
                        break;
                    case PlacementTypes.Line:
                        foreach (var coordinate in CurrentMode.LineCoordinates())
                        {
                            RequestPlacement(coordinate);
                        }
                        DeactivateSpecialPlacement();
                        break;
                    case PlacementTypes.Grid:
                        foreach (var coordinate in CurrentMode.GridCoordinates())
                        {
                            RequestPlacement(coordinate);
                        }
                        DeactivateSpecialPlacement();
                        break;
                }
            }
        }

        public void HandleDeletion(IEntity entity)
        {
            if (!IsActive || !Eraser) return;

            var msg = NetworkManager.CreateNetMessage<MsgPlacement>();
            msg.PlaceType = PlacementManagerMessage.RequestEntRemove;
            msg.EntityUid = entity.Uid;
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

        public void BeginPlacing(PlacementInformation info)
        {
            Clear();

            CurrentPermission = info;

            if (!_modeDictionary.Any(pair => pair.Key.Equals(CurrentPermission.PlacementOption)))
            {
                Clear();
                return;
            }

            Type modeType = _modeDictionary.First(pair => pair.Key.Equals(CurrentPermission.PlacementOption)).Value;
            CurrentMode = (PlacementMode)Activator.CreateInstance(modeType, this);

            if (info.IsTile)
                PreparePlacementTile((Tile)info.TileType);
            else
                PreparePlacement(info.EntityType);
        }

        public bool CurrentMousePosition(out ScreenCoordinates coordinates)
        {
            // Try to get current map.
            var map = MapId.Nullspace;
            var ent = PlayerManager.LocalPlayer.ControlledEntity;
            if (ent != null && ent.TryGetComponent<IGodotTransformComponent>(out var component))
            {
                map = component.MapID;
            }

            if (map == MapId.Nullspace || CurrentPermission == null || CurrentMode == null)
            {
                coordinates = new ScreenCoordinates(Vector2.Zero);
                return false;
            }

            coordinates = new ScreenCoordinates(inputManager.MouseScreenPosition);
            return true;
        }

        /// <inheritdoc />
        public void FrameUpdate(RenderFrameEventArgs e)
        {
            if (!CurrentMousePosition(out ScreenCoordinates mouseScreen))
            {
                return;
            }

            CurrentMode.AlignPlacementMode(mouseScreen);

            // purge old unapproved tile changes
            _pendingTileChanges.RemoveAll(c => c.Item2 < _time.RealTime);

            // continues tile placement but placement of entities only occurs on mouseup
            if (_placenextframe && CurrentPermission.IsTile)
            {
                HandlePlacement();
            }

            drawNode.Update();
        }

        /// <inheritdoc />
        public bool MouseDown(MouseButtonEventArgs e)
        {
            if (!IsActive || Eraser)
                return false;

            switch (e.Button)
            {
                case Mouse.Button.Left:
                    if (e.Shift)
                        ActivateLineMode(e);
                    else if (e.Control)
                        ActivateGridMode(e);
                    else
                        _placenextframe = true;
                    return true;
                case Mouse.Button.Right:
                    if (DeactivateSpecialPlacement())
                        return true;
                    Clear();
                    return true;
                case Mouse.Button.Middle:
                    Rotate();
                    return true;
            }

            return false;
        }

        private void ActivateLineMode(MouseButtonEventArgs e)
        {
            if (CurrentMode.HasLineMode)
            {
                if (!CurrentMousePosition(out ScreenCoordinates mouseScreen))
                {
                    return;
                }

                CurrentMode.AlignPlacementMode(mouseScreen);
                StartPoint = CurrentMode.MouseCoords;
                PlacementType = PlacementTypes.Line;
            }
        }

        private void ActivateGridMode(MouseButtonEventArgs e)
        {
            if (CurrentMode.HasGridMode)
            {
                if (!CurrentMousePosition(out ScreenCoordinates mouseScreen))
                {
                    return;
                }

                CurrentMode.AlignPlacementMode(mouseScreen);
                StartPoint = CurrentMode.MouseCoords;
                PlacementType = PlacementTypes.Grid;
            }
        }

        private bool DeactivateSpecialPlacement()
        {
            if (PlacementType != PlacementTypes.None)
            {
                PlacementType = PlacementTypes.None;
                return true;
            }
            return false;
        }

        public bool MouseUp(MouseButtonEventArgs e)
        {
            if (!IsActive || Eraser)
                return false;

            if (!_placenextframe)
            {
                return false;
            }
            //Places objects for nontile entities
            else if (!CurrentPermission.IsTile)
            {
                HandlePlacement();
            }

            _placenextframe = false;
            return true;
        }

        public void Render()
        {
            if (CurrentMode != null && IsActive)
            {
                CurrentMode.Render();

                if (CurrentPermission != null && CurrentPermission.Range > 0 && CurrentMode.RangeRequired)
                {
                    var pos = PlayerManager.LocalPlayer.ControlledEntity.GetComponent<ITransformComponent>().WorldPosition;
                    const int ppm = EyeManager.PIXELSPERMETER;
                    drawNode.DrawCircle(pos.Convert() * ppm, CurrentPermission.Range * ppm, new Godot.Color(1, 1, 1, 0.25f));
                }
            }
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

            CurrentBaseSprite = IconComponent.GetPrototypeIcon(prototype);
            CurrentPrototype = prototype;

            IsActive = true;
        }

        private void PreparePlacementTile(Tile tileType)
        {
            var tileDefs = _tileDefManager;

            CurrentBaseSprite = ResourceCache.GetResource<TextureResource>(new ResourcePath("/Textures/UserInterface/tilebuildoverlay.png")).Texture;

            IsActive = true;
        }

        private void RequestPlacement(GridLocalCoordinates coordinates)
        {
            if (coordinates.MapID == MapId.Nullspace) return;
            if (CurrentPermission == null) return;
            if (!CurrentMode.IsValidPosition(coordinates)) return;

            if (CurrentPermission.IsTile)
            {
                var grid = _mapMan.GetMap(coordinates.MapID).GetGrid(coordinates.GridID);
                var worldPos = coordinates.ToWorld();
                var localPos = worldPos.ConvertToGrid(grid);

                // no point changing the tile to the same thing.
                if (grid.GetTile(localPos).Tile.TileId == CurrentPermission.TileType)
                    return;

                foreach (var tileChange in _pendingTileChanges)
                {
                    // if change already pending, ignore it
                    if (tileChange.Item1 == localPos)
                        return;
                }

                var tuple = new Tuple<GridLocalCoordinates, TimeSpan>(localPos, _time.RealTime + _pendingTileTimeout);
                _pendingTileChanges.Add(tuple);
            }
;
            var message = NetworkManager.CreateNetMessage<MsgPlacement>();
            message.PlaceType = PlacementManagerMessage.RequestPlacement;

            message.Align = CurrentMode.ModeName;
            message.IsTile = CurrentPermission.IsTile;

            if (CurrentPermission.IsTile)
                message.TileType = CurrentPermission.TileType;
            else
                message.EntityTemplateName = CurrentPermission.EntityType;

            // world x and y
            message.XValue = coordinates.X;
            message.YValue = coordinates.Y;

            message.DirRcv = Direction;

            NetworkManager.ClientSendMessage(message);
        }

        public enum PlacementTypes
        {
            None = 0,
            Line = 1,
            Grid = 2
        }
    }
}
