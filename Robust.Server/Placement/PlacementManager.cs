using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;

namespace Robust.Server.Placement
{
    public class PlacementManager : IPlacementManager
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IServerNetManager _networkManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        //TO-DO: Expand for multiple permission per mob?
        //       Add support for multi-use placeables (tiles etc.).
        public List<PlacementInformation> BuildPermissions { get; set; } = new();

        //Holds build permissions for all mobs. A list of mobs and the objects they're allowed to request and how. One permission per mob.

        public Func<MsgPlacement, bool>? AllowPlacementFunc { get; set; }

        #region IPlacementManager Members

        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgPlacement>(HandleNetMessage);
        }

        /// <summary>
        ///  Handles placement related client messages.
        /// </summary>
        public void HandleNetMessage(MsgPlacement msg)
        {
            if (AllowPlacementFunc != null && !AllowPlacementFunc(msg))
            {
                return;
            }

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
                case PlacementManagerMessage.RequestRectRemove:
                    HandleRectRemoveReq(msg);
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

            var dirRcv = msg.DirRcv;

            var session = _playerManager.GetSessionByChannel(msg.MsgChannel);
            var plyEntity = session.AttachedEntity;

            // Don't have an entity, don't get to place.
            if (plyEntity == null)
                return;

            //TODO: Distance check, so you can't place things off of screen.

            var coordinates = msg.EntityCoordinates;

            if (!coordinates.IsValid(_entityManager))
            {
                Logger.WarningS("placement",
                    $"{session} tried to place {msg.ObjType} at invalid coordinate {coordinates}");
                return;
            }

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
                var created = _entityManager.SpawnEntity(entityTemplateName, coordinates);

                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(created).LocalRotation = dirRcv.ToAngle();
            }
            else
            {
                var mapCoords = coordinates.ToMap(_entityManager);
                PlaceNewTile(tileType, mapCoords.MapId, mapCoords.Position);
            }
        }

        private void PlaceNewTile(ushort tileType, MapId mapId, Vector2 position)
        {
            // tile can snap up to 0.75m away from grid
            var gridSearchBox = new Box2(-0.5f, -0.5f, 0.5f, 0.5f)
                .Scale(1.5f)
                .Translated(position);

            var gridsInArea = _mapManager.FindGridsIntersecting(mapId, gridSearchBox);

            IMapGrid? closest = null;
            float distance = float.PositiveInfinity;
            Box2 intersect = new Box2();
            foreach (var grid in gridsInArea)
            {
                // figure out closest intersect
                var gridIntersect = gridSearchBox.Intersect(grid.WorldBounds);
                var gridDist = (gridIntersect.Center - position).LengthSquared;

                if (gridDist >= distance)
                    continue;

                distance = gridDist;
                closest = grid;
                intersect = gridIntersect;
            }

            if (closest != null) // stick to existing grid
            {
                // round to nearest cardinal dir
                var deltaVec = position - intersect.Center;
                var normal = new Vector2(0,0);
                if (deltaVec != Vector2.Zero)
                {
                    normal = new Angle(deltaVec).GetCardinalDir().ToVec();
                }


                // round coords to center of tile
                var tileIndices = closest.WorldToTile(intersect.Center);
                var tileCenterWorld = closest.GridTileToWorldPos(tileIndices);

                // move mouse one tile out along normal
                var newTilePos = tileCenterWorld + normal * closest.TileSize;

                // you can always remove a tile
                if(Tile.Empty.TypeId != tileType)
                {
                    var tileBounds = Box2.UnitCentered.Scale(closest.TileSize).Translated(newTilePos);

                    var collideCount = _mapManager.FindGridsIntersecting(mapId, tileBounds).Count();

                    // prevent placing a tile if it overlaps more than one grid
                    if(collideCount > 1)
                        return;
                }

                var pos = closest.WorldToTile(position);
                closest.SetTile(pos, new Tile(tileType));
            }
            else if (tileType != 0) // create a new grid
            {
                var newGrid = _mapManager.CreateGrid(mapId);
                newGrid.WorldPosition = position + (newGrid.TileSize / 2f); // assume bottom left tile origin
                var tilePos = newGrid.WorldToTile(position);
                newGrid.SetTile(tilePos, new Tile(tileType));
            }
        }

        private void HandleEntRemoveReq(EntityUid entityUid)
        {
            //TODO: Some form of admin check
            if (_entityManager.TryGetEntity(entityUid, out var entity))
                _entityManager.DeleteEntity(entity);
        }

        private void HandleRectRemoveReq(MsgPlacement msg)
        {
            EntityCoordinates start = msg.EntityCoordinates;
            Vector2 rectSize = msg.RectSize;
            foreach (IEntity entity in IoCManager.Resolve<IEntityLookup>().GetEntitiesIntersecting(start.GetMapId(_entityManager),
                new Box2(start.Position, start.Position + rectSize)))
            {
                if ((!IoCManager.Resolve<IEntityManager>().EntityExists(entity) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity).EntityLifeStage) >= EntityLifeStage.Deleted || IoCManager.Resolve<IEntityManager>().HasComponent<IMapGridComponent>(entity) || IoCManager.Resolve<IEntityManager>().HasComponent<ActorComponent>(entity))
                    continue;
                IoCManager.Resolve<IEntityManager>().DeleteEntity((EntityUid) entity);
            }
        }

        /// <summary>
        ///  Places mob in entity placement mode with given settings.
        /// </summary>
        public void SendPlacementBegin(IEntity mob, int range, string objectType, string alignOption)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<ActorComponent?>(mob, out var actor))
                return;

            var playerConnection = actor.PlayerSession.ConnectedClient;
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
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<ActorComponent?>(mob, out var actor))
                return;

            var playerConnection = actor.PlayerSession.ConnectedClient;
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
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<ActorComponent?>(mob, out var actor))
                return;

            var playerConnection = actor.PlayerSession.ConnectedClient;
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
                MobUid = mob,
                Range = range,
                IsTile = false,
                EntityType = objectType,
                PlacementOption = alignOption
            };

            IEnumerable<PlacementInformation> mobPermissions = from PlacementInformation permission in BuildPermissions
                                                               where permission.MobUid == mob
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
                MobUid = mob,
                Range = range,
                IsTile = true,
                TileType = _tileDefinitionManager[tileType].TileId,
                PlacementOption = alignOption
            };

            IEnumerable<PlacementInformation> mobPermissions = from PlacementInformation permission in BuildPermissions
                                                               where permission.MobUid == mob
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
                .Where(permission => permission.MobUid == mob)
                .ToList();

            if (mobPermissions.Count != 0)
                BuildPermissions.RemoveAll(x => mobPermissions.Contains(x));
        }

        #endregion IPlacementManager Members

        private PlacementInformation? GetPermission(EntityUid uid, string alignOpt)
        {
            foreach (var buildPermission in BuildPermissions)
            {
                if (buildPermission.MobUid == uid && buildPermission.PlacementOption == alignOpt)
                {
                    return buildPermission;
                }
            }

            return null;
        }
    }
}
