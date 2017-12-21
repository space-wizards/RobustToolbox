using SS14.Server.GameObjects;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Server.Interfaces.Placement;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Maths;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Utility;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Server.Placement
{
    public class PlacementManager : IPlacementManager
    {
        //TO-DO: Expand for multiple permission per mob?
        //       Add support for multi-use placeables (tiles etc.).
        public List<PlacementInformation> BuildPermissions { get; set; } = new List<PlacementInformation>();

        //Holds build permissions for all mobs. A list of mobs and the objects they're allowed to request and how. One permission per mob.

        #region IPlacementManager Members


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
            }
        }

        public void HandlePlacementRequest(MsgPlacement msg)
        {
            var alignRcv = msg.Align;
            var isTile = msg.IsTile;
            var mapMgr = IoCManager.Resolve<IMapManager>();

            ushort tileType = 0;
            var entityTemplateName = "";

            if (isTile) tileType = msg.TileType;
            else entityTemplateName = msg.EntityTemplateName;

            float XValue = msg.XValue;
            float YValue = msg.YValue;
            int GridIndex = msg.GridIndex;
            int MapIndex = msg.MapIndex;
            var coordinates = new LocalCoordinates(XValue, YValue, GridIndex, MapIndex);

            var dirRcv = msg.DirRcv;

            IPlayerSession session = IoCManager.Resolve<IPlayerManager>().GetSessionByChannel(msg.MsgChannel);
            if (session.attachedEntity == null)
                return; //Don't accept placement requests from nobodys

            PlacementInformation permission = GetPermission(session.attachedEntity.Uid, alignRcv);

            float a = (float)Math.Floor(XValue);
            float b = (float)Math.Floor(YValue);
            Vector2 tilePos = new Vector2(a, b);

            if (permission != null || true)
            //isAdmin) Temporarily disable actual permission check / admin check. REENABLE LATER
            {
                if (permission != null)
                {
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
                }

                if (!isTile)
                {
                    var manager = IoCManager.Resolve<IServerEntityManager>();
                    if(manager.TrySpawnEntityAt(entityTemplateName, coordinates, out IEntity created))
                    {
                        created.GetComponent<TransformComponent>().WorldPosition =
                            new Vector2(XValue, YValue);
                        if (created.TryGetComponent<TransformComponent>(out var component))
                            component.Rotation = dirRcv.ToAngle();
                    }
                }
                else
                {
                    coordinates.Grid.SetTile(coordinates, new Tile(tileType));
                }
            }
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

            var net = IoCManager.Resolve<IServerNetManager>();
            var message = net.CreateNetMessage<MsgPlacement>();
            message.PlaceType = PlacementManagerMessage.StartPlacement;
            message.Range = range;
            message.IsTile = false;
            message.ObjType = objectType;
            message.AlignOption = alignOption;
            net.ServerSendMessage(message, playerConnection);
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

            var net = IoCManager.Resolve<IServerNetManager>();
            var message = net.CreateNetMessage<MsgPlacement>();

            message.PlaceType = PlacementManagerMessage.StartPlacement;
            message.Range = range;
            message.IsTile = true;
            message.ObjType = tileType;
            message.AlignOption = alignOption;

            net.ServerSendMessage(message, playerConnection);
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

            var net = IoCManager.Resolve<IServerNetManager>();
            var message = net.CreateNetMessage<MsgPlacement>();
            message.PlaceType = PlacementManagerMessage.CancelPlacement;
            net.ServerSendMessage(message, playerConnection);
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
                TileType = IoCManager.Resolve<ITileDefinitionManager>()[tileType].TileId,
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
            IEnumerable<PlacementInformation> mobPermissions = from PlacementInformation permission in BuildPermissions
                                                               where permission.MobUid == mob.Uid
                                                               select permission;

            if (mobPermissions.Any())
                BuildPermissions.RemoveAll(x => mobPermissions.Contains(x));
        }

        #endregion IPlacementManager Members

        private PlacementInformation GetPermission(int uid, string alignOpt)
        {
            IEnumerable<PlacementInformation> permission = from p in BuildPermissions
                                                           where p.MobUid == uid && p.PlacementOption.Equals(alignOpt)
                                                           select p;

            if (permission.Any()) return permission.First();
            else return null;
        }
    }
}
