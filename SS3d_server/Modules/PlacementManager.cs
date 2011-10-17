using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D_shared;
using SS3D_shared.HelperClasses;
using Lidgren.Network;
using SS3D_Server.Modules;
using SS3D_Server.Atom.Mob;
using SS3D_Server.Atom;
using System.Reflection;
using ServerServices;
using System.Drawing;
using ServerServices.Tiles;

namespace SS3D_Server.Modules
{
    class PlacementManager
    {
        //TO-DO: Expand for multiple permission per mob?
        //       Add support for multi-use placeables (tiles etc.).

        private Boolean editMode = false;               //If true, clients may freely request and place objects.

        public List<BuildPermission> BuildPermissions = new List<BuildPermission>(); //Holds build permissions for all mobs. A list of mobs and the objects they're allowed to request and how. One permission per mob.

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
            PlacementManagerMessage messageType = (PlacementManagerMessage)msg.ReadByte();

            switch (messageType)
            {
                case PlacementManagerMessage.StartPlacement:
                    break;
                case PlacementManagerMessage.CancelPlacement:
                    break;
                case PlacementManagerMessage.RequestPlacement:
                    HandlePlacementRequest(msg);
                    break;
                case PlacementManagerMessage.EDITMODE_ToggleEditMode: //THIS REALLY NEED ADMINCHECKS OR SOMETHING.
                    editMode = !editMode;
                    SS3D_Server.SS3DServer.Singleton.chatManager.SendChatMessage(ChatChannel.Server, "Edit Mode : " + (editMode ? "On" : "Off"), "", 0);
                    break;
                case PlacementManagerMessage.EDITMODE_GetObject:
                    if (editMode) HandleEditRequest(msg);
                    break;
            }
        }

        private BuildPermission GetPermission(int uid, AlignmentOptions alignOpt)
        {
            var permission = from p in BuildPermissions
                             where p.mobUid == uid && p.AlignOption == alignOpt
                             select p;

            if (permission.Any()) return permission.First();
            else return null;
        }

        public void HandleEditRequest(NetIncomingMessage msg)
        {
            string objectType = msg.ReadString();
            AlignmentOptions align = (AlignmentOptions)msg.ReadByte();
            Type fullType = SS3DServer.Singleton.atomManager.GetAtomType(objectType);
            if (fullType != null) StartBuilding(SS3DServer.Singleton.playerManager.GetSessionByConnection(msg.SenderConnection).attachedAtom, 120, objectType, align, editMode);
            else LogManager.Log("Invalid Object Requested : " + "SS3D_Server." + objectType);
        }

        public void HandlePlacementRequest(NetIncomingMessage msg)
        {
            AlignmentOptions alignRcv = (AlignmentOptions)msg.ReadByte();
            float xRcv = msg.ReadFloat();
            float yRcv = msg.ReadFloat();
            float rotRcv = msg.ReadFloat();

            if (GetPermission(SS3DServer.Singleton.playerManager.GetSessionByConnection(msg.SenderConnection).attachedAtom.Uid, alignRcv) != null)
            {
                //DO PLACEMENT CHECKS. Are they allowed to place this here?
                BuildPermission permission = GetPermission(SS3DServer.Singleton.playerManager.GetSessionByConnection(msg.SenderConnection).attachedAtom.Uid, alignRcv);

                if (!editMode)
                {
                    BuildPermissions.Remove(permission);
                    SendPlacementCancel(SS3DServer.Singleton.playerManager.GetSessionByConnection(msg.SenderConnection).attachedAtom);
                }

                Type objectType = SS3DServer.Singleton.atomManager.GetAtomType(permission.type);

                if (objectType.IsSubclassOf(typeof(Tile)))
                {
                    Point arrayPos = SS3D_Server.SS3DServer.Singleton.map.GetTileArrayPositionFromWorldPosition(new Vector2(xRcv, yRcv));
                    SS3D_Server.SS3DServer.Singleton.map.ChangeTile(arrayPos.X, arrayPos.Y, objectType);
                    SS3D_Server.SS3DServer.Singleton.map.NetworkUpdateTile(arrayPos.X, arrayPos.Y);
                }
                else
                {
                    SS3D_Server.SS3DServer.Singleton.atomManager.SpawnAtom(permission.type, new Vector2(xRcv, yRcv), rotRcv);
                }

            }
            else //They are not allowed to request this. Send 'PlacementFailed'. TBA
            {
                LogManager.Log("Invalid placement request: " 
                    + SS3DServer.Singleton.playerManager.GetSessionByConnection(msg.SenderConnection).name +
                    " - " + SS3DServer.Singleton.playerManager.GetSessionByConnection(msg.SenderConnection).attachedAtom.Uid.ToString() +
                    " - " + alignRcv.ToString());

                SendPlacementCancel(SS3DServer.Singleton.playerManager.GetSessionByConnection(msg.SenderConnection).attachedAtom);
            }
        }

        /// <summary>
        ///  Places mob in object placement mode with given settings.
        /// </summary>
        public void SendPlacementBegin(Atom.Atom mob, ushort range, string objectType, AlignmentOptions alignOption, bool placeAnywhere)
        {
            NetOutgoingMessage message = SS3DNetServer.Singleton.CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.StartPlacement);
            message.Write(range);
            message.Write(objectType);
            message.Write((byte)alignOption);
            message.Write(placeAnywhere);
            //This looks like a large message but its just a string, ushort and a bunch of bools.
            SS3DNetServer.Singleton.SendMessage(message, mob.attachedClient, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///  Cancels object placement mode for given mob.
        /// </summary>
        public void SendPlacementCancel(Atom.Atom mob)
        {
            NetOutgoingMessage message = SS3DNetServer.Singleton.CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.CancelPlacement);
            SS3DNetServer.Singleton.SendMessage(message, mob.attachedClient, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///  Gives Mob permission to place object and places it in object placement mode.
        /// </summary>
        public void StartBuilding(Atom.Atom mob, ushort range, string objectType, AlignmentOptions alignOption, bool placeAnywhere)
        {
            AssignBuildPermission(mob, range, objectType, alignOption, placeAnywhere);
            SendPlacementBegin(mob, range, objectType, alignOption, placeAnywhere);
        }

        /// <summary>
        ///  Revokes open placement Permission and cancels object placement mode.
        /// </summary>
        public void CancelBuilding(Atom.Atom mob)
        {
            RevokeAllBuildPermissions(mob);
            SendPlacementCancel(mob);
        }

        /// <summary>
        ///  Gives a mob a permission to place a given object.
        /// </summary>
        public void AssignBuildPermission(Atom.Atom mob, ushort range, string objectType, AlignmentOptions alignOption, bool placeAnywhere)
        {
            BuildPermission newPermission = new BuildPermission();
            newPermission.mobUid = mob.Uid;
            newPermission.range = range;
            newPermission.type = objectType;
            newPermission.AlignOption = alignOption;
            newPermission.placeAnywhere = placeAnywhere;

            var mobPermissions = from BuildPermission permission in BuildPermissions
                                 where permission.mobUid == mob.Uid
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
        public void RevokeAllBuildPermissions(Atom.Atom mob)
        {
            var mobPermissions = from BuildPermission permission in BuildPermissions
                                 where permission.mobUid == mob.Uid
                                 select permission;

            if (mobPermissions.Any())
                BuildPermissions.RemoveAll(x => mobPermissions.Contains(x));
        }
    }
}
