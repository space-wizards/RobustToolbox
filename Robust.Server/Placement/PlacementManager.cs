using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Collections;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;

namespace Robust.Server.Placement
{
    public sealed class PlacementManager : IPlacementManager
    {
        [Dependency] private readonly IComponentFactory _factory = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IServerNetManager _networkManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
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
            var plyEntity = _entityManager.GetComponentOrNull<TransformComponent>(session.AttachedEntity);

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
                // Replace existing entities if relevant.
                if (msg.Replacement && _prototype.Index<EntityPrototype>(entityTemplateName).Components.TryGetValue(
                        _factory.GetComponentName(typeof(PlacementReplacementComponent)), out var compRegistry))
                {
                    var key = ((PlacementReplacementComponent)compRegistry.Component).Key;
                    var gridUid = coordinates.GetGridUid(_entityManager);

                    if (_entityManager.TryGetComponent<MapGridComponent>(gridUid, out var grid))
                    {
                        var replacementQuery = _entityManager.GetEntityQuery<PlacementReplacementComponent>();
                        var anc = grid.GetAnchoredEntitiesEnumerator(grid.LocalToTile(coordinates));
                        var toDelete = new ValueList<EntityUid>();

                        while (anc.MoveNext(out var ent))
                        {
                            if (!replacementQuery.TryGetComponent(ent, out var repl) ||
                                repl.Key != key)
                            {
                                continue;
                            }

                            toDelete.Add(ent.Value);
                        }

                        foreach (var ent in toDelete)
                        {
                            _entityManager.DeleteEntity(ent);
                        }
                    }
                }

                var created = _entityManager.SpawnEntity(entityTemplateName, coordinates);

                _entityManager.GetComponent<TransformComponent>(created).LocalRotation = dirRcv.ToAngle();
            }
            else
            {
                PlaceNewTile(tileType, coordinates);
            }
        }

        private void PlaceNewTile(ushort tileType, EntityCoordinates coordinates)
        {
            if (!coordinates.IsValid(_entityManager)) return;

            MapGridComponent? grid;

            _mapManager.TryGetGrid(coordinates.EntityId, out grid);

            if (grid == null)
                _mapManager.TryFindGridAt(coordinates.ToMap(_entityManager), out _, out grid);

            if (grid != null)  // stick to existing grid
            {
                grid.SetTile(coordinates, new Tile(tileType));
            }
            else if (tileType != 0) // create a new grid
            {
                var newGrid = _mapManager.CreateGrid(coordinates.GetMapId(_entityManager));
                var newGridXform = _entityManager.GetComponent<TransformComponent>(newGrid.Owner);
                newGridXform.WorldPosition = coordinates.Position - (newGrid.TileSize / 2f); // assume bottom left tile origin
                var tilePos = newGrid.WorldToTile(coordinates.Position);
                newGrid.SetTile(tilePos, new Tile(tileType));
            }
        }

        private void HandleEntRemoveReq(EntityUid entityUid)
        {
            //TODO: Some form of admin check
            if (_entityManager.EntityExists(entityUid))
                _entityManager.DeleteEntity(entityUid);
        }

        private void HandleRectRemoveReq(MsgPlacement msg)
        {
            EntityCoordinates start = msg.EntityCoordinates;
            Vector2 rectSize = msg.RectSize;
            foreach (EntityUid entity in EntitySystem.Get<EntityLookupSystem>().GetEntitiesIntersecting(start.GetMapId(_entityManager),
                new Box2(start.Position, start.Position + rectSize)))
            {
                if (_entityManager.Deleted(entity) || _entityManager.HasComponent<MapGridComponent>(entity) || _entityManager.HasComponent<ActorComponent>(entity))
                    continue;
                _entityManager.DeleteEntity(entity);
            }
        }

        /// <summary>
        ///  Places mob in entity placement mode with given settings.
        /// </summary>
        public void SendPlacementBegin(EntityUid mob, int range, string objectType, string alignOption)
        {
            if (!_entityManager.TryGetComponent<ActorComponent?>(mob, out var actor))
                return;

            var playerConnection = actor.PlayerSession.ConnectedClient;
            if (playerConnection == null)
                return;

            var message = new MsgPlacement();
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
        public void SendPlacementBeginTile(EntityUid mob, int range, string tileType, string alignOption)
        {
            if (!_entityManager.TryGetComponent<ActorComponent?>(mob, out var actor))
                return;

            var playerConnection = actor.PlayerSession.ConnectedClient;
            if (playerConnection == null)
                return;

            var message = new MsgPlacement();
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
        public void SendPlacementCancel(EntityUid mob)
        {
            if (!_entityManager.TryGetComponent<ActorComponent?>(mob, out var actor))
                return;

            var playerConnection = actor.PlayerSession.ConnectedClient;
            if (playerConnection == null)
                return;

            var message = new MsgPlacement();
            message.PlaceType = PlacementManagerMessage.CancelPlacement;
            _networkManager.ServerSendMessage(message, playerConnection);
        }

        /// <summary>
        ///  Gives Mob permission to place entity and places it in object placement mode.
        /// </summary>
        public void StartBuilding(EntityUid mob, int range, string objectType, string alignOption)
        {
            AssignBuildPermission(mob, range, objectType, alignOption);
            SendPlacementBegin(mob, range, objectType, alignOption);
        }

        /// <summary>
        ///  Gives Mob permission to place tile and places it in object placement mode.
        /// </summary>
        public void StartBuildingTile(EntityUid mob, int range, string tileType, string alignOption)
        {
            AssignBuildPermission(mob, range, tileType, alignOption);
            SendPlacementBeginTile(mob, range, tileType, alignOption);
        }

        /// <summary>
        ///  Revokes open placement Permission and cancels object placement mode.
        /// </summary>
        public void CancelBuilding(EntityUid mob)
        {
            RevokeAllBuildPermissions(mob);
            SendPlacementCancel(mob);
        }

        /// <summary>
        ///  Gives a mob a permission to place a given Entity.
        /// </summary>
        public void AssignBuildPermission(EntityUid mob, int range, string objectType, string alignOption)
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
        public void AssignBuildPermissionTile(EntityUid mob, int range, string tileType, string alignOption)
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
        public void RevokeAllBuildPermissions(EntityUid mob)
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
