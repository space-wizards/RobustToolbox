using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprites;
using SS14.Shared.Interfaces.Map;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.Map;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.ResourceManagement;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Maths;
using SS14.Shared.Network.Messages;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Network;

namespace SS14.Client.Placement
{
    public class PlacementManager : IPlacementManager
    {
        [Dependency]
        public readonly ICollisionManager CollisionManager;
        [Dependency]
        public readonly IClientNetManager NetworkManager;
        [Dependency]
        public readonly IPlayerManager PlayerManager;
        [Dependency]
        public readonly IResourceCache ResourceCache;
        [Dependency]
        public readonly IReflectionManager ReflectionManager;
        private readonly Dictionary<string, Type> _modeDictionary = new Dictionary<string, Type>();

        public bool IsActive { get; private set; }
        public bool Eraser { get; private set; }
        public Sprite CurrentBaseSprite { get; set; }
        public string CurrentBaseSpriteKey { get; set; } = "";
        public PlacementMode CurrentMode { get; set; }
        public PlacementInformation CurrentPermission { get; set; }
        public EntityPrototype CurrentPrototype { get; set; }
        public Direction Direction { get; set; } = Direction.South;
        public bool ValidPosition { get; set; }

        public PlacementManager()
        {
            Clear();
        }

        #region IPlacementManager Members

        public void Initialize()
        {
            NetworkManager.RegisterNetMessage<MsgPlacement>(MsgPlacement.NAME, HandlePlacementMessage);
            

            _modeDictionary.Clear();
            foreach (var type in ReflectionManager.GetAllChildren<PlacementMode>())
            {
                _modeDictionary.Add(type.Name, type);
            }
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
        
        public event EventHandler PlacementCanceled;

        public void Clear()
        {
            CurrentBaseSprite = null;
            CurrentPrototype = null;
            CurrentPermission = null;
            CurrentMode = null;
            if (PlacementCanceled != null && IsActive && !Eraser) PlacementCanceled(this, null);
            IsActive = false;
            Eraser = false;
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

            IoCManager.Resolve<IUserInterfaceManager>().DragInfo.Reset();

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

        public void Update(ScreenCoordinates mouseScreen)
        {
            if (mouseScreen.MapID == MapId.Nullspace || CurrentPermission == null || CurrentMode == null) return;

            ValidPosition = CurrentMode.Update(mouseScreen);
        }

        public void Render()
        {
            if (CurrentMode != null)
            {
                CurrentMode.Render();

                if (CurrentPermission != null && CurrentPermission.Range > 0 && CurrentMode.rangerequired)
                {
                    var pos = CluwneLib.WorldToScreen(PlayerManager.LocalPlayer.ControlledEntity.GetComponent<ITransformComponent>().WorldPosition);
                    CluwneLib.drawHollowCircle((int)Math.Floor(pos.X),
                        (int)Math.Floor(pos.Y),
                        CurrentPermission.Range * CluwneLib.Camera.PixelsPerMeter,
                        3f,
                        Color4.White);
                }
            }
        }

        #endregion IPlacementManager Members

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

            ComponentParameter spriteParam = prototype.GetBaseSpriteParamaters().FirstOrDefault();
            //Will break if states not ordered correctly.

            var spriteName = spriteParam == null ? "" : spriteParam.GetValue<string>();
            Sprite sprite = ResourceCache.GetSprite(spriteName);

            CurrentBaseSprite = sprite;
            CurrentBaseSpriteKey = spriteName;
            CurrentPrototype = prototype;

            IsActive = true;
        }

        private void PreparePlacementTile(Tile tileType)
        {
            var tileDefs = IoCManager.Resolve<ITileDefinitionManager>();

            CurrentBaseSprite = ResourceCache.GetSprite("tilebuildoverlay");
            CurrentBaseSpriteKey = "tilebuildoverlay";

            IsActive = true;
        }

        private void RequestPlacement()
        {
            if (CurrentPermission == null) return;
            if (!ValidPosition) return;

            var message = NetworkManager.CreateNetMessage<MsgPlacement>();
            message.PlaceType = PlacementManagerMessage.RequestPlacement;

            message.Align = CurrentMode.ModeName;
            message.IsTile = CurrentPermission.IsTile;

            if (CurrentPermission.IsTile) message.TileType = CurrentPermission.TileType;
            else message.EntityTemplateName = CurrentPermission.EntityType;

            message.XValue = CurrentMode.MouseCoords.X;
            message.YValue = CurrentMode.MouseCoords.Y;
            message.GridIndex = CurrentMode.MouseCoords.GridID;
            message.MapIndex = CurrentMode.MouseCoords.MapID;

            message.DirRcv = Direction;

            NetworkManager.ClientSendMessage(message);
        }

        public Sprite GetDirectionalSprite()
        {
            Sprite spriteToUse = CurrentBaseSprite;

            if (CurrentBaseSprite == null) return null;

            string dirName = (CurrentBaseSpriteKey + "_" + Direction).ToLowerInvariant();

            if (ResourceCache.TryGetResource(dirName, out SpriteResource spriteRes))
                spriteToUse = spriteRes.Sprite;

            return spriteToUse;
        }
    }
}
