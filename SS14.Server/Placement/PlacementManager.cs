using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Server.Interfaces.Placement;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Maths;
using SS14.Shared.Network.Messages;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Server.Placement
{
    public class PlacementManager : IPlacementManager
    {
        [Dependency]
        private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency]
        private readonly IServerNetManager _networkManager;
        [Dependency]
        private readonly IPlayerManager _playerManager;
        [Dependency]
        private readonly IServerEntityManager _entityManager;
        [Dependency]
        private readonly IMapManager _mapManager;

        //TO-DO: Expand for multiple permission per mob?
        //       Add support for multi-use placeables (tiles etc.).
        public List<PlacementInformation> BuildPermissions { get; set; } = new List<PlacementInformation>();

        //Holds build permissions for all mobs. A list of mobs and the objects they're allowed to request and how. One permission per mob.

        #region IPlacementManager Members

        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgPlacement>(MsgPlacement.NAME, message => HandleNetMessage((MsgPlacement)message));
        }

        /// <summary>
        ///  Handles placement related client messages.
        /// </summary>
        public void HandleNetMessage(MsgPlacement msg)
        {
            switch (msg.PlaceType)
            {
                case PlacementManagerMessage.StartPlacement:
                    break;
                case PlacementManagerMessage.CancelPlacement:
                    break;
                case PlacementManagerMessage.RequestPlacement:
                    HandlePlacementRequest(msg);
                    break;
                case PlacementManagerMessage.RequestEntRemove:
                    HandleEntRemoveReq(msg.EntityUid);
                    break;
            }
        }

        public void HandlePlacementRequest(MsgPlacement msg)
        {
            var alignRcv = msg.Align;
            var isTile = msg.IsTile;

            ushort tileType = 0;
            var entityTemplateName = "";

            if (isTile) tileType = msg.TileType;
            else entityTemplateName = msg.EntityTemplateName;

            var xValue = msg.XValue;
            var yValue = msg.YValue;
            var dirRcv = msg.DirRcv;

            var session = _playerManager.GetSessionByChannel(msg.MsgChannel);
            var plyEntity = session.AttachedEntity;

            // Don't have an entity, don't get to place.
            if (plyEntity == null)
                return;

            // get the MapID the player is on
            var plyTransform = plyEntity.GetComponent<ITransformComponent>();
            var mapIndex = plyTransform.MapID;

            // no building in null space!
            if(mapIndex == MapId.Nullspace)
                return;

            //TODO: Distance check, so you can't place things off of screen.

            // get the grid under the worldCoords.
            var grid = _mapManager.GetMap(mapIndex).FindGridAt(new Vector2(xValue, yValue));
            var coordinates = new LocalCoordinates(xValue, yValue, grid.Index, mapIndex);


            /* TODO: Redesign permission system, or document what this is supposed to be doing
            var permission = GetPermission(session.attachedEntity.Uid, alignRcv);
            if (permission == null)
                return;

            if (permission.Uses > 0)
            {
                permission.Uses--;
                if (permission.Uses <= 0)
                {
                    BuildPermissions.Remove(permission);
                    SendPlacementCancel(session.attachedEntity);
                }
            }
            else
            {
                BuildPermissions.Remove(permission);
                SendPlacementCancel(session.attachedEntity);
                return;
            }
            */
            if (!isTile)
            {
                if (!_entityManager.TrySpawnEntityAt(entityTemplateName, coordinates, out IEntity created))
                    return;

                var transform = created.GetComponent<TransformComponent>();
                transform.WorldPosition = new Vector2(xValue, yValue);
                if (created.TryGetComponent<TransformComponent>(out var component))
                    component.LocalRotation = dirRcv.ToAngle();
            }
            else
            {
                coordinates.Grid.SetTile(coordinates, new Tile(tileType));
            }
        }

        private void HandleEntRemoveReq(EntityUid entityUid)
        {
            //TODO: Some form of admin check
            if (_entityManager.TryGetEntity(entityUid, out var entity))
                _entityManager.DeleteEntity(entity);
        }

        /// <summary>
        ///  Places mob in entity placement mode with given settings.
        /// </summary>
        public void SendPlacementBegin(IEntity mob, int range, string objectType, string alignOption)
        {
            if (!mob.TryGetComponent<IActorComponent>(out var actor))
                return;

            var playerConnection = actor.playerSession.ConnectedClient;
            if (playerConnection == null)
                return;

            var message = _networkManager.CreateNetMessage<MsgPlacement>();
            message.PlaceType = PlacementManagerMessage.StartPlacement;
            message.Range = range;
            message.IsTile = false;
            message.ObjType = objectType;
            message.AlignOption = alignOption;
            _networkManager.ServerSendMessage(message, playerConnection);
        }

        /// <summary>
        ///  Places mob in tile placement mode with given settings.
        /// </summary>
        public void SendPlacementBeginTile(IEntity mob, int range, string tileType, string alignOption)
        {
            if (!mob.TryGetComponent<IActorComponent>(out var actor))
                return;

            var playerConnection = actor.playerSession.ConnectedClient;
            if (playerConnection == null)
                return;

            var message = _networkManager.CreateNetMessage<MsgPlacement>();
            message.PlaceType = PlacementManagerMessage.StartPlacement;
            message.Range = range;
            message.IsTile = true;
            message.ObjType = tileType;
            message.AlignOption = alignOption;
            _networkManager.ServerSendMessage(message, playerConnection);
        }

        /// <summary>
        ///  Cancels object placement mode for given mob.
        /// </summary>
        public void SendPlacementCancel(IEntity mob)
        {
            if (!mob.TryGetComponent<IActorComponent>(out var actor))
                return;

            var playerConnection = actor.playerSession.ConnectedClient;
            if (playerConnection == null)
                return;

            var message = _networkManager.CreateNetMessage<MsgPlacement>();
            message.PlaceType = PlacementManagerMessage.CancelPlacement;
            _networkManager.ServerSendMessage(message, playerConnection);
        }

        /// <summary>
        ///  Gives Mob permission to place entity and places it in object placement mode.
        /// </summary>
        public void StartBuilding(IEntity mob, int range, string objectType, string alignOption)
        {
            AssignBuildPermission(mob, range, objectType, alignOption);
            SendPlacementBegin(mob, range, objectType, alignOption);
        }

        /// <summary>
        ///  Gives Mob permission to place tile and places it in object placement mode.
        /// </summary>
        public void StartBuildingTile(IEntity mob, int range, string tileType, string alignOption)
        {
            AssignBuildPermission(mob, range, tileType, alignOption);
            SendPlacementBeginTile(mob, range, tileType, alignOption);
        }

        /// <summary>
        ///  Revokes open placement Permission and cancels object placement mode.
        /// </summary>
        public void CancelBuilding(IEntity mob)
        {
            RevokeAllBuildPermissions(mob);
            SendPlacementCancel(mob);
        }

        /// <summary>
        ///  Gives a mob a permission to place a given Entity.
        /// </summary>
        public void AssignBuildPermission(IEntity mob, int range, string objectType, string alignOption)
        {
            var newPermission = new PlacementInformation
            {
                MobUid = mob.Uid,
                Range = range,
                IsTile = false,
                EntityType = objectType,
                PlacementOption = alignOption
            };

            IEnumerable<PlacementInformation> mobPermissions = from PlacementInformation permission in BuildPermissions
                                                               where permission.MobUid == mob.Uid
                                                               select permission;

            if (mobPermissions.Any()) //Already has one? Revoke the old one and add this one.
            {
                RevokeAllBuildPermissions(mob);
                BuildPermissions.Add(newPermission);
            }
            else
            {
                BuildPermissions.Add(newPermission);
            }
        }

        /// <summary>
        ///  Gives a mob a permission to place a given Tile.
        /// </summary>
        public void AssignBuildPermissionTile(IEntity mob, int range, string tileType, string alignOption)
        {
            var newPermission = new PlacementInformation
            {
                MobUid = mob.Uid,
                Range = range,
                IsTile = true,
                TileType = _tileDefinitionManager[tileType].TileId,
                PlacementOption = alignOption
            };

            IEnumerable<PlacementInformation> mobPermissions = from PlacementInformation permission in BuildPermissions
                                                               where permission.MobUid == mob.Uid
                                                               select permission;

            if (mobPermissions.Any()) //Already has one? Revoke the old one and add this one.
            {
                RevokeAllBuildPermissions(mob);
                BuildPermissions.Add(newPermission);
            }
            else
            {
                BuildPermissions.Add(newPermission);
            }
        }

        /// <summary>
        ///  Removes all building Permissions for given mob.
        /// </summary>
        public void RevokeAllBuildPermissions(IEntity mob)
        {
            var mobPermissions = BuildPermissions
                .Where(permission => permission.MobUid == mob.Uid)
                .ToList();

            if (mobPermissions.Any())
                BuildPermissions.RemoveAll(x => mobPermissions.Contains(x));
        }

        #endregion IPlacementManager Members

        private PlacementInformation GetPermission(EntityUid uid, string alignOpt)
        {
            var permission = BuildPermissions
                .Where(p => p.MobUid == uid && p.PlacementOption.Equals(alignOpt))
                .ToList();

            return permission.Any() ? permission.First() : null;
        }
    }
}
