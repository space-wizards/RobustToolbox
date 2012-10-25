using System;
using System.Collections.Generic;
using System.Linq;
using SS13_Shared;
using Lidgren.Network;
using System.Drawing;
using SS13_Shared.GO;
using ServerInterfaces.Placement;
using ServerServices.Log;
using SS13.IoC;
using ServerInterfaces.Network;
using ServerInterfaces.Player;
using ServerInterfaces.GameObject;
using ServerInterfaces;
using ServerInterfaces.Map;
using ServerServices.Map;

namespace ServerServices.Placement
{
    public class PlacementManager : IPlacementManager
    {
        //TO-DO: Expand for multiple permission per mob?
        //       Add support for multi-use placeables (tiles etc.).
        public List<PlacementInformation> BuildPermissions = new List<PlacementInformation>(); //Holds build permissions for all mobs. A list of mobs and the objects they're allowed to request and how. One permission per mob.
        private ISS13Server _server;

        public void Initialize(ISS13Server server)
        {
            _server = server;
        }

        /// <summary>
        ///  Handles placement related client messages.
        /// </summary>
        public void HandleNetMessage(NetIncomingMessage msg)
        {
            var messageType = (PlacementManagerMessage)msg.ReadByte();

            switch (messageType)
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

        private PlacementInformation GetPermission(int uid, PlacementOption alignOpt)
        {
            var permission = from p in BuildPermissions
                             where p.MobUid == uid && p.PlacementOption == alignOpt
                             select p;

            if (permission.Any()) return permission.First();
            else return null;
        }

        public void HandlePlacementRequest(NetIncomingMessage msg)
        {
            PlacementOption alignRcv = (PlacementOption)msg.ReadByte();

            Boolean isTile = msg.ReadBoolean();

            Map.MapManager mapMgr = (Map.MapManager)IoCManager.Resolve<IMapManager>();

            string tileType = null;

            string entityTemplateName = "";

            if (isTile) tileType = mapMgr.GetTileString(msg.ReadByte());
            else entityTemplateName = msg.ReadString();

            float xRcv = msg.ReadFloat();
            float yRcv = msg.ReadFloat();
            Direction dirRcv = (Direction)msg.ReadByte();

            int tileX = msg.ReadInt32();
            int tileY = msg.ReadInt32();

            IPlayerSession session = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection);
            PlacementInformation permission = GetPermission(session.attachedEntity.Uid, alignRcv);
            Boolean isAdmin = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection).adminPermissions.isAdmin;

            if (permission != null || true) //isAdmin) Temporarily disable actual permission check / admin check. REENABLE LATER
            {
                if (permission != null)
                {
                    if(permission.Uses > 0)
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
                    IEntity created = _server.EntityManager.SpawnEntityAt(entityTemplateName, new Vector2(xRcv, yRcv));
                    if (created != null)
                    {
                        created.Translate(new Vector2(xRcv, yRcv));
                        created.Direction = dirRcv;
                        created.SendMessage(this, ComponentMessageType.WallMountTile, new Vector2(tileX, tileY));
                    }
                }
                else
                {
                    Point arrayPos = IoCManager.Resolve<IMapManager>().GetTileArrayPositionFromWorldPosition(new Vector2(xRcv, yRcv));
                    mapMgr.ChangeTile(arrayPos.X, arrayPos.Y, tileType);
                    mapMgr.NetworkUpdateTile(arrayPos.X, arrayPos.Y);
                }
            }
            else //They are not allowed to request this. Send 'PlacementFailed'. TBA
            {
                LogManager.Log("Invalid placement request: "
                    + IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection).name +
                    " - " + IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection).attachedEntity.Uid.ToString() +
                    " - " + alignRcv.ToString());

                SendPlacementCancel(IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection).attachedEntity);
            }
        }

        /// <summary>
        ///  Places mob in entity placement mode with given settings.
        /// </summary>
        public void SendPlacementBegin(IEntity mob, ushort range, string objectType, PlacementOption alignOption)
        {
            var message = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.StartPlacement);
            message.Write(range);
            message.Write(false);//Not a tile
            message.Write(objectType);
            message.Write((byte)alignOption);

            var reply = mob.SendMessage(this, ComponentFamily.Actor, ComponentMessageType.GetActorConnection);
            if (reply.MessageType == ComponentMessageType.ReturnActorConnection)
                IoCManager.Resolve<ISS13NetServer>().SendMessage(message, (NetConnection)reply.ParamsList[0], NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///  Places mob in tile placement mode with given settings.
        /// </summary>
        public void SendPlacementBeginTile(IEntity mob, ushort range, string tileType, PlacementOption alignOption)
        {
            var mapMgr = (MapManager)IoCManager.Resolve<IMapManager>();
            var message = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.StartPlacement);
            message.Write(range);
            message.Write(true);//Is a tile.
            message.Write(mapMgr.GetTileIndex(tileType));
            message.Write((byte)alignOption);

            var reply = mob.SendMessage(this, ComponentFamily.Actor,ComponentMessageType.GetActorConnection);
            if (reply.MessageType == ComponentMessageType.ReturnActorConnection)
                IoCManager.Resolve<ISS13NetServer>().SendMessage(message, (NetConnection)reply.ParamsList[0], NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///  Cancels object placement mode for given mob.
        /// </summary>
        public void SendPlacementCancel(IEntity mob)
        {
            var message = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.CancelPlacement);

            var reply = mob.SendMessage(this, ComponentFamily.Actor, ComponentMessageType.GetActorConnection);
            if (reply.MessageType == ComponentMessageType.ReturnActorConnection)
                IoCManager.Resolve<ISS13NetServer>().SendMessage(message, (NetConnection)reply.ParamsList[0], NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///  Gives Mob permission to place entity and places it in object placement mode.
        /// </summary>
        public void StartBuilding(IEntity mob, ushort range, string objectType, PlacementOption alignOption)
        {
            AssignBuildPermission(mob, range, objectType, alignOption);
            SendPlacementBegin(mob, range, objectType, alignOption);
        }

        /// <summary>
        ///  Gives Mob permission to place tile and places it in object placement mode.
        /// </summary>
        public void StartBuildingTile(IEntity mob, ushort range, string tileType, PlacementOption alignOption)
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
        public void AssignBuildPermission(IEntity mob, ushort range, string objectType, PlacementOption alignOption)
        {
            PlacementInformation newPermission = new PlacementInformation();
            newPermission.MobUid = mob.Uid;
            newPermission.Range = range;
            newPermission.IsTile = false;
            newPermission.EntityType = objectType;
            newPermission.PlacementOption = alignOption;

            var mobPermissions = from PlacementInformation permission in BuildPermissions
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
        public void AssignBuildPermissionTile(IEntity mob, ushort range, string tileType, PlacementOption alignOption)
        {
            PlacementInformation newPermission = new PlacementInformation();
            newPermission.MobUid = mob.Uid;
            newPermission.Range = range;
            newPermission.IsTile = true;
            newPermission.TileType = tileType;
            newPermission.PlacementOption = alignOption;

            var mobPermissions = from PlacementInformation permission in BuildPermissions
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
            var mobPermissions = from PlacementInformation permission in BuildPermissions
                                 where permission.MobUid == mob.Uid
                                 select permission;

            if (mobPermissions.Any())
                BuildPermissions.RemoveAll(x => mobPermissions.Contains(x));
        }
    }
}
