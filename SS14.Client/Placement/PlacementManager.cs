using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Collision;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Map;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SS14.Client.Placement
{
    [IoCTarget]
    public class PlacementManager : IPlacementManager
    {
        public readonly ICollisionManager CollisionManager;
        public readonly INetworkManager NetworkManager;
        public readonly IPlayerManager PlayerManager;
        public readonly IResourceManager ResourceManager;
        private readonly Dictionary<string, Type> _modeDictionary = new Dictionary<string, Type>();

        public Sprite CurrentBaseSprite;
        public string CurrentBaseSpriteKey = "";
        public PlacementMode CurrentMode;
        public PlacementInformation CurrentPermission;
        public EntityPrototype CurrentPrototype;
        public Direction Direction = Direction.South;
        public bool ValidPosition;

        public PlacementManager(IResourceManager resourceManager, INetworkManager networkManager,
                                ICollisionManager collisionManager, IPlayerManager playerManager)
        {
            ResourceManager = resourceManager;
            NetworkManager = networkManager;
            CollisionManager = collisionManager;
            PlayerManager = playerManager;

            Type type = typeof (PlacementMode);
            List<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            List<Type> types = assemblies.SelectMany(t => t.GetTypes()).Where(p => type.IsAssignableFrom(p)).ToList();

            _modeDictionary.Clear();
            foreach (Type t in types)
                _modeDictionary.Add(t.Name, t);

            Clear();
        }

        #region IPlacementManager Members

        public Boolean IsActive { get; private set; }
        public Boolean Eraser { get; private set; }

        public event EventHandler PlacementCanceled;

        public void HandleNetMessage(NetIncomingMessage msg)
        {
            var messageType = (PlacementManagerMessage) msg.ReadByte();

            switch (messageType)
            {
                case PlacementManagerMessage.StartPlacement:
                    HandleStartPlacement(msg);
                    break;
                case PlacementManagerMessage.CancelPlacement:
                    Clear();
                    break;
                case PlacementManagerMessage.PlacementFailed:
                    //Sad trombone here.
                    break;
            }
        }

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

        public void HandleDeletion(Entity entity)
        {
            if (!IsActive || !Eraser) return;

            NetOutgoingMessage message = NetworkManager.CreateMessage();
            message.Write((byte) NetMessage.RequestEntityDeletion);
            message.Write(entity.Uid);
            NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
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
            CurrentMode = (PlacementMode) Activator.CreateInstance(modeType, this);

            if (info.IsTile)
                PreparePlacementTile((Tile)info.TileType);
            else
                PreparePlacement(info.EntityType);
        }

        public void Update(Vector2i mouseScreen, IMapManager currentMap)
        {
            if (currentMap == null || CurrentPermission == null || CurrentMode == null) return;

            ValidPosition = CurrentMode.Update(mouseScreen, currentMap);
        }

        public void Render()
        {
            if (CurrentMode != null)
                CurrentMode.Render();

            if (CurrentPermission != null && CurrentPermission.Range > 0)
            {
                var pos = CluwneLib.WorldToScreen(PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
                CluwneLib.drawCircle(                    pos.X,
                    pos.Y,
                    CurrentPermission.Range,
                    Color.White,
                    new Vector2f(2, 2));
            }
        }

        #endregion

        private void HandleStartPlacement(NetIncomingMessage msg)
        {
            CurrentPermission = new PlacementInformation
                                    {
                                        Range = msg.ReadInt32(),
                                        IsTile = msg.ReadBoolean()
                                    };

            if (CurrentPermission.IsTile) CurrentPermission.TileType = msg.ReadUInt16();
            else CurrentPermission.EntityType = msg.ReadString();
            CurrentPermission.PlacementOption = msg.ReadString();

            BeginPlacing(CurrentPermission);
        }

        private void PreparePlacement(string templateName)
        {
            EntityPrototype prototype =
                IoCManager.Resolve<IPrototypeManager>().Index<EntityPrototype>(templateName);

            ComponentParameter spriteParam = prototype.GetBaseSpriteParamaters().FirstOrDefault();
            //Will break if states not ordered correctly.
            //if (spriteParam == null) return;

            var spriteName = spriteParam == null?"":spriteParam.GetValue<string>();
            Sprite sprite = ResourceManager.GetSprite(spriteName);

            CurrentBaseSprite = sprite;
            CurrentBaseSpriteKey = spriteName;
            CurrentPrototype = prototype;

            IsActive = true;
        }

        private void PreparePlacementTile(Tile tileType)
        {
            if (tileType.TileDef.IsWall)
            {
                CurrentBaseSprite = ResourceManager.GetSprite("wall");
                CurrentBaseSpriteKey = "wall";
            }
            else
            {
                CurrentBaseSprite = ResourceManager.GetSprite("tilebuildoverlay");
                CurrentBaseSpriteKey = "tilebuildoverlay";
            }

            IsActive = true;
        }

        private void RequestPlacement()
        {
            if (CurrentPermission == null) return;
            if (!ValidPosition) return;
            
            NetOutgoingMessage message = NetworkManager.CreateMessage();

            message.Write((byte) NetMessage.PlacementManagerMessage);
            message.Write((byte) PlacementManagerMessage.RequestPlacement);
            message.Write(CurrentMode.ModeName);

            message.Write(CurrentPermission.IsTile);

            if (CurrentPermission.IsTile) message.Write(CurrentPermission.TileType);
            else message.Write(CurrentPermission.EntityType);

            message.Write(CurrentMode.mouseWorld.X);
            message.Write(CurrentMode.mouseWorld.Y);

            message.Write((byte) Direction);

            NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public Sprite GetDirectionalSprite()
        {
            Sprite spriteToUse = CurrentBaseSprite;

            if (CurrentBaseSprite == null) return null;

            string dirName = (CurrentBaseSpriteKey + "_" + Direction.ToString()).ToLowerInvariant();
            if (ResourceManager.SpriteExists(dirName))
                spriteToUse = ResourceManager.GetSprite(dirName);

            return spriteToUse;
        }
    }
}
