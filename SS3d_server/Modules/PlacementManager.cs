using System;
using System.Collections.Generic;
using System.Linq;
using SS13_Shared;
using Lidgren.Network;
using ServerServices;
using System.Drawing;
using SGO;
using SS13_Shared.GO;

namespace SS13_Server.Modules
{
    class PlacementManager
    {
        //TO-DO: Expand for multiple permission per mob?
        //       Add support for multi-use placeables (tiles etc.).
        public List<PlacementInformation> BuildPermissions = new List<PlacementInformation>(); //Holds build permissions for all mobs. A list of mobs and the objects they're allowed to request and how. One permission per mob.

        #region Singleton
        private static PlacementManager singleton;

        private PlacementManager() { }

        public static PlacementManager Singleton
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new PlacementManager();
                }
                return singleton;
            }
        } 
        #endregion

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

            TileType tileType = TileType.None;
            string entityTemplateName = "";

            if (isTile) tileType = (TileType)msg.ReadInt32();
            else entityTemplateName = msg.ReadString();

            float xRcv = msg.ReadFloat();
            float yRcv = msg.ReadFloat();
            float rotRcv = msg.ReadFloat();

            PlayerSession session = SS13Server.Singleton.PlayerManager.GetSessionByConnection(msg.SenderConnection);
            PlacementInformation permission = GetPermission(session.attachedEntity.Uid, alignRcv);
            Boolean isAdmin = SS13Server.Singleton.PlayerManager.GetSessionByConnection(msg.SenderConnection).adminPermissions.isAdmin;

            if (permission != null || true) //isAdmin)
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
                    Entity created = EntityManager.Singleton.SpawnEntityAt(entityTemplateName, new Vector2(xRcv, yRcv));
                    if(created != null)
                        created.Translate(new Vector2(xRcv, yRcv), rotRcv);
                }
                else
                {
                    Point arrayPos = SS13_Server.SS13Server.Singleton.Map.GetTileArrayPositionFromWorldPosition(new Vector2(xRcv, yRcv));
                    SS13_Server.SS13Server.Singleton.Map.ChangeTile(arrayPos.X, arrayPos.Y, tileType);
                    SS13_Server.SS13Server.Singleton.Map.NetworkUpdateTile(arrayPos.X, arrayPos.Y);
                }
            }
            else //They are not allowed to request this. Send 'PlacementFailed'. TBA
            {
                LogManager.Log("Invalid placement request: "
                    + SS13Server.Singleton.PlayerManager.GetSessionByConnection(msg.SenderConnection).name +
                    " - " + SS13Server.Singleton.PlayerManager.GetSessionByConnection(msg.SenderConnection).attachedEntity.Uid.ToString() +
                    " - " + alignRcv.ToString());

                SendPlacementCancel(SS13Server.Singleton.PlayerManager.GetSessionByConnection(msg.SenderConnection).attachedEntity);
            }
        }

        /// <summary>
        ///  Places mob in entity placement mode with given settings.
        /// </summary>
        public void SendPlacementBegin(Entity mob, ushort range, string objectType, PlacementOption alignOption)
        {
            var message = SS13NetServer.Singleton.CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.StartPlacement);
            message.Write(range);
            message.Write(false);//Not a tile
            message.Write(objectType);
            message.Write((byte)alignOption);

            var reply = mob.SendMessage(this, ComponentFamily.Actor, ComponentMessageType.GetActorConnection);
            if (reply.MessageType == ComponentMessageType.ReturnActorConnection)
                SS13NetServer.Singleton.SendMessage(message, (NetConnection)reply.ParamsList[0], NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///  Places mob in tile placement mode with given settings.
        /// </summary>
        public void SendPlacementBegin(Entity mob, ushort range, TileType tileType, PlacementOption alignOption)
        {
            var message = SS13NetServer.Singleton.CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.StartPlacement);
            message.Write(range);
            message.Write(true);//Is a tile.
            message.Write((int)tileType);
            message.Write((byte)alignOption);

            var reply = mob.SendMessage(this, ComponentFamily.Actor,ComponentMessageType.GetActorConnection);
            if (reply.MessageType == ComponentMessageType.ReturnActorConnection)
                SS13NetServer.Singleton.SendMessage(message, (NetConnection)reply.ParamsList[0], NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///  Cancels object placement mode for given mob.
        /// </summary>
        public void SendPlacementCancel(Entity mob)
        {
            var message = SS13NetServer.Singleton.CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.CancelPlacement);

            var reply = mob.SendMessage(this, ComponentFamily.Actor, ComponentMessageType.GetActorConnection);
            if (reply.MessageType == ComponentMessageType.ReturnActorConnection)
                SS13NetServer.Singleton.SendMessage(message, (NetConnection)reply.ParamsList[0], NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///  Gives Mob permission to place entity and places it in object placement mode.
        /// </summary>
        public void StartBuilding(Entity mob, ushort range, string objectType, PlacementOption alignOption)
        {
            AssignBuildPermission(mob, range, objectType, alignOption);
            SendPlacementBegin(mob, range, objectType, alignOption);
        }

        /// <summary>
        ///  Gives Mob permission to place tile and places it in object placement mode.
        /// </summary>
        public void StartBuilding(Entity mob, ushort range, TileType tileType, PlacementOption alignOption)
        {
            AssignBuildPermission(mob, range, tileType, alignOption);
            SendPlacementBegin(mob, range, tileType, alignOption);
        }

        /// <summary>
        ///  Revokes open placement Permission and cancels object placement mode.
        /// </summary>
        public void CancelBuilding(Entity mob)
        {
            RevokeAllBuildPermissions(mob);
            SendPlacementCancel(mob);
        }

        /// <summary>
        ///  Gives a mob a permission to place a given Entity.
        /// </summary>
        public void AssignBuildPermission(Entity mob, ushort range, string objectType, PlacementOption alignOption)
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
        public void AssignBuildPermission(Entity mob, ushort range, TileType tileType, PlacementOption alignOption)
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
        public void RevokeAllBuildPermissions(Entity mob)
        {
            var mobPermissions = from PlacementInformation permission in BuildPermissions
                                 where permission.MobUid == mob.Uid
                                 select permission;

            if (mobPermissions.Any())
                BuildPermissions.RemoveAll(x => mobPermissions.Contains(x));
        }
    }
}
