using GorgonLibrary;
using GorgonLibrary.Graphics;
using Lidgren.Network;
using SS14.Client.ClientWindow;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Collision;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.Map;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace SS14.Client.Services.Placement
{
    public class PlacementManager : IPlacementManager
    {
        public readonly ICollisionManager CollisionManager;
        public readonly INetworkManager NetworkManager;
        public readonly IPlayerManager PlayerManager;
        public readonly IResourceManager ResourceManager;
        private readonly Dictionary<string, Type> _modeDictionary = new Dictionary<string, Type>();

        public Sprite CurrentBaseSprite;
        public PlacementMode CurrentMode;
        public PlacementInformation CurrentPermission;
        public EntityTemplate CurrentTemplate;
        public Direction Direction = Direction.South;
        public Boolean ValidPosition;

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
            CurrentTemplate = null;
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

            IoCManager.Resolve<IUserInterfaceManager>().CancelTargeting();
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
                PreparePlacementTile(info.TileType);
            else
                PreparePlacement(info.EntityType);
        }

        public void Update(Vector2D mouseScreen, IMapManager currentMap)
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
                Gorgon.CurrentRenderTarget.Circle(
                    PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.
                        X - ClientWindowData.Singleton.ScreenOrigin.X,
                    PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.
                        Y - ClientWindowData.Singleton.ScreenOrigin.Y,
                    CurrentPermission.Range,
                    Color.White,
                    new Vector2D(2, 2));
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

            var mapMgr = (MapManager) IoCManager.Resolve<IMapManager>();

            if (CurrentPermission.IsTile) CurrentPermission.TileType = mapMgr.GetTileString(msg.ReadByte());
            else CurrentPermission.EntityType = msg.ReadString();
            CurrentPermission.PlacementOption = msg.ReadString();

            BeginPlacing(CurrentPermission);
        }

        private void PreparePlacement(string templateName)
        {
            EntityTemplate template =
                IoCManager.Resolve<IEntityManagerContainer>().EntityManager.EntityTemplateDatabase.GetTemplate(
                    templateName);
            if (template == null) return;

            ComponentParameter spriteParam = template.GetBaseSpriteParamaters().FirstOrDefault();
            //Will break if states not ordered correctly.
            //if (spriteParam == null) return;

            var spriteName = spriteParam == null?"":spriteParam.GetValue<string>();
            Sprite sprite = ResourceManager.GetSprite(spriteName);

            CurrentBaseSprite = sprite;
            CurrentTemplate = template;

            IsActive = true;
        }

        private void PreparePlacementTile(string tileType)
        {
            if (tileType == "Wall")
            {
                CurrentBaseSprite = ResourceManager.GetSprite("wall");
            }
            else
            {
                CurrentBaseSprite = ResourceManager.GetSprite("tilebuildoverlay");
            }

            IsActive = true;
        }

        private void RequestPlacement()
        {
            if (CurrentPermission == null) return;
            if (!ValidPosition) return;

            var mapMgr = (MapManager) IoCManager.Resolve<IMapManager>();
            NetOutgoingMessage message = NetworkManager.CreateMessage();

            message.Write((byte) NetMessage.PlacementManagerMessage);
            message.Write((byte) PlacementManagerMessage.RequestPlacement);
            message.Write(CurrentMode.ModeName);

            message.Write(CurrentPermission.IsTile);

            if (CurrentPermission.IsTile) message.Write(mapMgr.GetTileIndex(CurrentPermission.TileType));
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

            string dirName = (CurrentBaseSprite.Name + "_" + Direction.ToString()).ToLowerInvariant();
            if (ResourceManager.SpriteExists(dirName))
                spriteToUse = ResourceManager.GetSprite(dirName);

            return spriteToUse;
        }
    }
}