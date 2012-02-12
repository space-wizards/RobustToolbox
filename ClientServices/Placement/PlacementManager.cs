using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.Collision;
using ClientInterfaces.GOC;
using ClientInterfaces.Map;
using ClientInterfaces.Network;
using ClientInterfaces.Placement;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientWindow;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using Lidgren.Network;
using SS13_Shared;
using System.Drawing;
using ClientInterfaces;
using CGO;

namespace ClientServices.Placement
{
    class PlacementManager : IPlacementManager
    {
        private readonly IResourceManager _resourceManager;
        private readonly INetworkManager _networkManager;
        private readonly ICollisionManager _collisionManager;
        private readonly IPlayerManager _playerManager;

        private const float SnapToRange = 55;
        private Sprite _currentSprite;
        private float _currentRotation;
        private EntityTemplate _currentTemplate;
        private PlacementInformation _currentPermission;
        private Boolean _validPosition;

        private Vector2D _currentLocScreen = Vector2D.Zero;
        private Vector2D _currentLocWorld = Vector2D.Zero;

        public Boolean IsActive { get; private set; }
        public Boolean Eraser { get; private set; }

        public event EventHandler PlacementCanceled;

        public PlacementManager(IResourceManager resourceManager, INetworkManager networkManager, ICollisionManager collisionManager, IPlayerManager playerManager)
        {
            _resourceManager = resourceManager;
            _networkManager = networkManager;
            _collisionManager = collisionManager;
            _playerManager = playerManager;

            Clear();
        }

        public void HandleNetMessage(NetIncomingMessage msg)
        {
            var messageType = (PlacementManagerMessage)msg.ReadByte();

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

        private void HandleStartPlacement(NetIncomingMessage msg)
        {
            _currentPermission = new PlacementInformation
                                     {
                                         Range = msg.ReadUInt16(),
                                         IsTile = msg.ReadBoolean()
                                     };

            if (_currentPermission.IsTile) _currentPermission.TileType = (TileType)msg.ReadInt32();
            else _currentPermission.EntityType = msg.ReadString();
            _currentPermission.PlacementOption = (PlacementOption)msg.ReadByte();

            BeginPlacing(_currentPermission);
        }

        public void Clear()
        {
            _currentSprite = null;
            _currentRotation = 0;
            _currentTemplate = null;
            _currentPermission = null;
            _currentLocScreen = Vector2D.Zero;
            _currentLocWorld = Vector2D.Zero;
            if (PlacementCanceled != null && IsActive && !Eraser) PlacementCanceled(this, null);
            IsActive = false;
            Eraser = false;
        }

        public void HandlePlacement()
        {
            if (IsActive && !Eraser)
                RequestPlacement();
        }

        public void HandleDeletion(IEntity entity)
        {
            if (!IsActive || !Eraser) return;

            var message = _networkManager.CreateMessage();
            message.Write((byte)NetMessage.RequestEntityDeletion);
            message.Write(entity.Uid);
            _networkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
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

            _currentPermission = info;

            if (info.IsTile)
                PreparePlacement(info.TileType);
            else
                PreparePlacement(info.EntityType);
        }

        private void PreparePlacement(string templateName)
        {
            var template = EntityManager.Singleton.TemplateDB.GetTemplate(templateName);
            if (template == null) return;

            var spriteParam = template.GetBaseSpriteParamaters().FirstOrDefault(); //Will break if states not ordered correctly.
            if (spriteParam == null) return;

            var spriteName = (string)spriteParam.Parameter;
            var sprite = _resourceManager.GetSprite(spriteName);

            _currentSprite = sprite;
            _currentTemplate = template;
            _currentRotation = 0;

            IsActive = true;
        }

        private void PreparePlacement(TileType tileType)
        {
            _currentSprite = _resourceManager.GetSprite("tilebuildoverlay");

            IsActive = true;
        }

        private void RequestPlacement()
        {
            if (_currentPermission == null) return;
            if (!_validPosition) return;

            NetOutgoingMessage message = _networkManager.CreateMessage();

            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.RequestPlacement);
            message.Write((byte)PlacementOption.AlignNone);

            message.Write(_currentPermission.IsTile);

            if (_currentPermission.IsTile) message.Write((int)_currentPermission.TileType);
            else message.Write(_currentPermission.EntityType);

            message.Write(_currentLocWorld.X);
            message.Write(_currentLocWorld.Y);
            message.Write(_currentRotation);

            _networkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public void Update(Vector2D mouseScreen, IMapManager currentMap)
        {
            if (currentMap == null) return;

            _currentLocScreen = mouseScreen;

            _currentLocWorld = new Vector2D(mouseScreen.X + ClientWindowData.Singleton.ScreenOrigin.X, mouseScreen.Y + ClientWindowData.Singleton.ScreenOrigin.Y);

            _validPosition = true;

            if (_currentPermission != null)
            {
                var spriteRectWorld = new RectangleF(_currentLocWorld.X - (_currentSprite.Width / 2f), _currentLocWorld.Y - (_currentSprite.Height / 2f), _currentSprite.Width, _currentSprite.Height);

                #region AlignNone
                if (_currentPermission.PlacementOption == PlacementOption.AlignNone || _currentPermission.PlacementOption == PlacementOption.AlignNoneFree)
                {
                    if (_currentPermission.IsTile)
                    {
                        _validPosition = false;
                        return;
                    }

                    if (_collisionManager.IsColliding(spriteRectWorld, true)) _validPosition = false;

                    if (currentMap.IsSolidTile(_currentLocWorld)) _validPosition = false; //HANDLE CURSOR OUTSIDE MAP

                    if (_currentPermission.PlacementOption == PlacementOption.AlignNone) //AlignNoneFree does not check for range.
                        if ((_playerManager.ControlledEntity.Position - _currentLocWorld).Length > _currentPermission.Range) _validPosition = false;
                } 
                #endregion

                #region AlignSimilar
                else if (_currentPermission.PlacementOption == PlacementOption.AlignSimilar || _currentPermission.PlacementOption == PlacementOption.AlignSimilarFree)
                {
                    if (_currentPermission.IsTile)
                    {
                        _validPosition = false;
                        return;
                    }

                    //Align to similar if nearby found else free
                    if (currentMap.IsSolidTile(_currentLocWorld)) _validPosition = false; //HANDLE CURSOR OUTSIDE MAP

                    if (_currentPermission.PlacementOption == PlacementOption.AlignSimilar)
                        if ((_playerManager.ControlledEntity.Position - _currentLocWorld).Length > _currentPermission.Range) _validPosition = false;

                    
                    var nearbyEntities = EntityManager.Singleton.GetEntitiesInRange(_currentLocWorld, SnapToRange);

                    var snapToEntities = from IEntity entity in nearbyEntities
                                         where entity.Template == _currentTemplate
                                         orderby (entity.Position - _currentLocWorld).Length ascending
                                         select entity;

                    if (snapToEntities.Any())
                    {
                        var closestEntity = snapToEntities.First();
                        var replies = new List<ComponentReplyMessage>();
                        closestEntity.SendMessage(this, SS13_Shared.GO.ComponentMessageType.GetSprite, replies);

                        //if(replies.Any(x => x.messageType == SS13_Shared.GO.ComponentMessageType.CurrentSprite))
                        //{
                        //    Sprite closestSprite = (Sprite)replies.Find(x => x.messageType == SS13_Shared.GO.ComponentMessageType.CurrentSprite).paramsList[0]; //This is safer but slower.

                        if (replies.Any())
                        {
                            var closestSprite = (Sprite)replies.First().ParamsList[0]; //This is faster but kinda unsafe.

                            var closestRect = new RectangleF(closestEntity.Position.X - closestSprite.Width / 2f, closestEntity.Position.Y - closestSprite.Height / 2f, closestSprite.Width, closestSprite.Height);

                            var sides = new List<Vector2D>
                                            {
                                                new Vector2D(closestRect.X + (closestRect.Width/2f), closestRect.Top - _currentSprite.Height/2f),
                                                new Vector2D(closestRect.X + (closestRect.Width/2f), closestRect.Bottom + _currentSprite.Height/2f),
                                                new Vector2D(closestRect.Left - _currentSprite.Width/2f, closestRect.Y + (closestRect.Height/2f)),
                                                new Vector2D(closestRect.Right + _currentSprite.Width/2f, closestRect.Y + (closestRect.Height/2f))
                                            };

                            var closestSide = (from Vector2D side in sides orderby (side - _currentLocWorld).Length ascending select side).First();

                            _currentLocWorld = closestSide;
                            _currentLocScreen = new Vector2D(closestSide.X - ClientWindowData.Singleton.ScreenOrigin.X, closestSide.Y - ClientWindowData.Singleton.ScreenOrigin.Y);
                        }
                    }

                    spriteRectWorld = new RectangleF(_currentLocWorld.X - (_currentSprite.Width / 2f), _currentLocWorld.Y - (_currentSprite.Height / 2f), _currentSprite.Width, _currentSprite.Height);
                    if (_collisionManager.IsColliding(spriteRectWorld, true)) _validPosition = false;
                }
                #endregion

                #region AlignTile
                else if (_currentPermission.PlacementOption == PlacementOption.AlignTileAny ||
                 _currentPermission.PlacementOption == PlacementOption.AlignTileAnyFree ||
                 _currentPermission.PlacementOption == PlacementOption.AlignTileEmpty ||
                 _currentPermission.PlacementOption == PlacementOption.AlignTileEmptyFree ||
                 _currentPermission.PlacementOption == PlacementOption.AlignTileNonSolid ||
                 _currentPermission.PlacementOption == PlacementOption.AlignTileNonSolidFree ||
                 _currentPermission.PlacementOption == PlacementOption.AlignTileSolid ||
                 _currentPermission.PlacementOption == PlacementOption.AlignTileSolidFree)
                {
                    if (_currentPermission.PlacementOption == PlacementOption.AlignTileAny ||
                        _currentPermission.PlacementOption == PlacementOption.AlignTileEmpty ||
                        _currentPermission.PlacementOption == PlacementOption.AlignTileNonSolid ||
                        _currentPermission.PlacementOption == PlacementOption.AlignTileSolid)
                        if ((_playerManager.ControlledEntity.Position - _currentLocWorld).Length > _currentPermission.Range) _validPosition = false;

                    if (_currentPermission.PlacementOption == PlacementOption.AlignTileNonSolid || _currentPermission.PlacementOption == PlacementOption.AlignTileNonSolidFree)
                        if (currentMap.IsSolidTile(_currentLocWorld)) _validPosition = false;

                    if (_currentPermission.PlacementOption == PlacementOption.AlignTileSolid || _currentPermission.PlacementOption == PlacementOption.AlignTileSolidFree)
                        if (!currentMap.IsSolidTile(_currentLocWorld)) _validPosition = false;

                    if (_currentPermission.PlacementOption == PlacementOption.AlignTileEmpty || _currentPermission.PlacementOption == PlacementOption.AlignTileEmptyFree)
                        _validPosition = _validPosition; //TBA.

                    if (_validPosition)
                    {
                        var currentTile = currentMap.GetTileAt(_currentLocWorld);
                        if (currentTile != null)
                        {
                            if (_currentPermission.IsTile)
                            {
                                _currentLocWorld = (currentTile.Position + new Vector2D(currentMap.GetTileSpacing() / 2f, currentMap.GetTileSpacing() / 2f));
                                _currentLocScreen = new Vector2D(_currentLocWorld.X - ClientWindowData.Singleton.ScreenOrigin.X, _currentLocWorld.Y - ClientWindowData.Singleton.ScreenOrigin.Y);

                                //TileRectWorld = new RectangleF(current_loc_world.X - (current_sprite.Width / 2f), current_loc_world.Y - (current_sprite.Height / 2f), current_sprite.Width, current_sprite.Height);
                                //CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
                                //if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false; //This also includes walls. Meaning that even when set to solid only this will be unplacable. Fix this.
                            }
                            else
                            {
                                _currentLocWorld = (currentTile.Position + new Vector2D(currentMap.GetTileSpacing() / 2f, currentMap.GetTileSpacing() / 2f)) + new Vector2D(_currentTemplate.PlacementOffset.Key, _currentTemplate.PlacementOffset.Value);
                                _currentLocScreen = new Vector2D(_currentLocWorld.X - ClientWindowData.Singleton.ScreenOrigin.X, _currentLocWorld.Y - ClientWindowData.Singleton.ScreenOrigin.Y);

                                spriteRectWorld = new RectangleF(_currentLocWorld.X - (_currentSprite.Width / 2f), _currentLocWorld.Y - (_currentSprite.Height / 2f), _currentSprite.Width, _currentSprite.Height);
                                if (_collisionManager.IsColliding(spriteRectWorld, true)) _validPosition = false; //This also includes walls. Meaning that even when set to solid only this will be unplacable. Fix this.
                            }
                        }
                        else _validPosition = false;
                    }

                }  
                #endregion

                #region AlignWall
                else if (_currentPermission.PlacementOption == PlacementOption.AlignWall || _currentPermission.PlacementOption == PlacementOption.AlignWallFree)
                {
                    if (_currentPermission.IsTile)
                    {
                        _validPosition = false;
                        return;
                    }

                    //CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
                    //if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false;

                    if (!currentMap.IsSolidTile(_currentLocWorld)) _validPosition = false;

                    if (_currentPermission.PlacementOption == PlacementOption.AlignWall)
                        if ((_playerManager.ControlledEntity.Position - _currentLocWorld).Length > _currentPermission.Range) _validPosition = false;

                    if (_validPosition)
                    {
                        var currentTile = currentMap.GetTileAt(_currentLocWorld);
                        var nodes = new List<Vector2D>();

                        if (_currentTemplate.MountingPoints != null)
                        {
                            nodes.AddRange(_currentTemplate.MountingPoints.Select(current => new Vector2D(_currentLocWorld.X, currentTile.Position.Y + current)));
                        }
                        else
                        {
                            nodes.Add(new Vector2D(_currentLocWorld.X, currentTile.Position.Y + 16));
                            nodes.Add(new Vector2D(_currentLocWorld.X, currentTile.Position.Y + 32));
                            nodes.Add(new Vector2D(_currentLocWorld.X, currentTile.Position.Y + 48));
                        }

                        Vector2D closestNode = (from Vector2D node in nodes
                                                orderby (node - _currentLocWorld).Length ascending
                                                select node).First();

                        _currentLocWorld = Vector2D.Add(closestNode, new Vector2D(_currentTemplate.PlacementOffset.Key, _currentTemplate.PlacementOffset.Value));
                        _currentLocScreen = new Vector2D(_currentLocWorld.X - ClientWindowData.Singleton.ScreenOrigin.X, _currentLocWorld.Y - ClientWindowData.Singleton.ScreenOrigin.Y);

                        if (_currentPermission.PlacementOption == PlacementOption.AlignWall)
                            if ((_playerManager.ControlledEntity.Position - _currentLocWorld).Length > _currentPermission.Range) _validPosition = false;
                    }
                } 
                #endregion

                else if (_currentPermission.PlacementOption == PlacementOption.Freeform)
                {
                    _validPosition = true; //Herpderp
                }
            }
        }

        public void Render()
        {
            if (_currentSprite != null)
            {
                _currentSprite.Color = _validPosition ? Color.ForestGreen : Color.IndianRed;
                _currentSprite.Position = new Vector2D(_currentLocScreen.X - (_currentSprite.Width / 2f), _currentLocScreen.Y - (_currentSprite.Height / 2f)); //Centering the sprite on the cursor.
                _currentSprite.Draw();
                _currentSprite.Color = Color.White;
            }

            if (_currentPermission != null)
            {
                if (_currentPermission.PlacementOption == PlacementOption.AlignNone    ||
                    _currentPermission.PlacementOption == PlacementOption.AlignSimilar ||
                    _currentPermission.PlacementOption == PlacementOption.AlignTileAny ||
                    _currentPermission.PlacementOption == PlacementOption.AlignTileEmpty ||
                    _currentPermission.PlacementOption == PlacementOption.AlignTileNonSolid ||
                    _currentPermission.PlacementOption == PlacementOption.AlignTileSolid ||
                    _currentPermission.PlacementOption == PlacementOption.AlignWall)   //If it uses range, show the range.
                {
                    Gorgon.Screen.Circle(
                        _playerManager.ControlledEntity.Position.X - ClientWindowData.Singleton.ScreenOrigin.X,
                        _playerManager.ControlledEntity.Position.Y - ClientWindowData.Singleton.ScreenOrigin.Y,
                        _currentPermission.Range,
                        Color.DeepSkyBlue,
                        new Vector2D(2, 2));
                }
            }
        }
            
    }
}
