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
        private readonly ISceneTreeHolder sceneTree;
        [Dependency]
        readonly public IInputManager inputManager;

        /// <summary>
        ///     How long before a pending tile change is dropped.
        /// </summary>
        private static readonly TimeSpan _pendingTileTimeout = TimeSpan.FromSeconds(2.0);

        private readonly Dictionary<string, Type> _modeDictionary = new Dictionary<string, Type>();
        private readonly List<Tuple<LocalCoordinates, TimeSpan>> _pendingTileChanges = new List<Tuple<LocalCoordinates, TimeSpan>>();

        private bool _tileMouseDown;

        public bool IsActive { get; private set; }
        public bool Eraser { get; private set; }
        public TextureResource CurrentBaseSprite { get; set; }
        public string CurrentBaseSpriteKey { get; set; } = "";
        public PlacementMode CurrentMode { get; set; }
        public PlacementInformation CurrentPermission { get; set; }
        public EntityPrototype CurrentPrototype { get; set; }
        public Direction Direction { get; set; } = Direction.South;
        public bool ValidPosition { get; set; }
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
            drawNode = new Godot.Node2D()
            {
                Name = "Placement Manager Sprite",
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

        private void HandlePlacementMessage(NetMessage netMessage)
        {
            var msg = (MsgPlacement) netMessage;
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
            if (PlacementCanceled != null && IsActive && !Eraser) PlacementCanceled(this, null);
            _tileMouseDown = false;
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
        }

        public void HandlePlacement()
        {
            if (IsActive && !Eraser)
                RequestPlacement();
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

        /// <inheritdoc />
        public void FrameUpdate(RenderFrameEventArgs e)
        {
            // Try to get current map.
            var map = MapId.Nullspace;
            var ent = PlayerManager.LocalPlayer.ControlledEntity;
            if (ent != null && ent.TryGetComponent<IClientTransformComponent>(out var component))
            {
                map = component.MapID;
            }

            if (map == MapId.Nullspace || CurrentPermission == null || CurrentMode == null)
            {
                return;
            }

            var mouseScreen = new ScreenCoordinates(inputManager.MouseScreenPosition, map);

            ValidPosition = CurrentMode.FrameUpdate(e, mouseScreen);

            // purge old unapproved tile changes
            _pendingTileChanges.RemoveAll(c => c.Item2 < _time.RealTime);

            // continues tile placement but placement of entities only occurs on mouseup
            if (_tileMouseDown && CurrentPermission.IsTile)
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
                    _tileMouseDown = true;
                    return true;
                case Mouse.Button.Right:
                    Clear();
                    return true;
                case Mouse.Button.Middle:
                    Rotate();
                    return true;
            }

            return false;
        }

        public bool MouseUp(MouseButtonEventArgs e)
        {
            if (!IsActive || Eraser)
                return false;

            if (!_tileMouseDown)
            {
                return false;
            }
            //Places objects for nontile entities
            else if(!CurrentPermission.IsTile)
            {
                HandlePlacement();
            }

            _tileMouseDown = false;
            return true;
        }

        public void Render()
        {
            if (CurrentMode != null && IsActive)
            {
                CurrentMode.Render();


                if (CurrentPermission != null && CurrentPermission.Range > 0 && CurrentMode.rangerequired)
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
            EntityPrototype prototype =
                IoCManager.Resolve<IPrototypeManager>().Index<EntityPrototype>(templateName);

            ComponentParameter spriteParam = prototype.GetBaseSpriteParameters().FirstOrDefault();
            //Will break if states not ordered correctly.

            var spriteName = spriteParam == null ? "" : spriteParam.GetValue<string>();
            var sprite = ResourceCache.GetResource<TextureResource>("Textures/" + spriteName);

            CurrentBaseSprite = sprite;
            CurrentBaseSpriteKey = spriteName;
            CurrentPrototype = prototype;

            IsActive = true;
        }

        private void PreparePlacementTile(Tile tileType)
        {
            var tileDefs = IoCManager.Resolve<ITileDefinitionManager>();

            CurrentBaseSprite = ResourceCache.GetResource<TextureResource>("Textures/UserInterface/tilebuildoverlay.png");
            CurrentBaseSpriteKey = "tilebuildoverlay";

            IsActive = true;
        }

        private void RequestPlacement()
        {
            if(CurrentMode.MouseCoords.MapID == MapId.Nullspace) return;
            if (CurrentPermission == null) return;
            if (!ValidPosition) return;

            if(CurrentPermission.IsTile)
            {
                var grid = _mapMan.GetMap(CurrentMode.MouseCoords.MapID).FindGridAt(new Vector2(CurrentMode.MouseCoords.X, CurrentMode.MouseCoords.Y));
                var worldPos = CurrentMode.MouseCoords;
                var localPos = worldPos.ConvertToGrid(grid);

                // no point changing the tile to the same thing.
                if(grid.GetTile(localPos).Tile.TileId == CurrentPermission.TileType)
                    return;

                foreach (var tileChange in _pendingTileChanges)
                {
                    // if change already pending, ignore it
                    if(tileChange.Item1 == localPos)
                        return;
                }

                var tuple = new Tuple<LocalCoordinates, TimeSpan>(localPos, _time.RealTime + _pendingTileTimeout);
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
            message.XValue = CurrentMode.MouseCoords.X;
            message.YValue = CurrentMode.MouseCoords.Y;

            message.DirRcv = Direction;

            NetworkManager.ClientSendMessage(message);
        }

        public TextureResource GetDirectionalSprite()
        {
            var spriteToUse = CurrentBaseSprite;

            if (CurrentBaseSprite == null) return null;

            string dirName = (CurrentBaseSpriteKey + "_" + Direction).ToLowerInvariant();

            if (ResourceCache.TryGetResource<TextureResource>(dirName, out var spriteRes))
                return spriteRes;

            return spriteToUse;
        }
    }
}

