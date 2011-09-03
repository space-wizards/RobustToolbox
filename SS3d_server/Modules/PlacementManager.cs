using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D_shared;
using SS3D_shared.HelperClasses;
using SS3D_Server.Modules.Map;
using Lidgren.Network;
using SS3D_Server.Modules;
using SS3D_Server.Atom.Mob;
using SS3D_Server.Atom;

namespace SS3D_Server.Modules
{
    class PlacementManager
    {
        //TO-DO: Expand for multiple permission per mob?
        //       Add support for multi-use placeables (tiles etc.).

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
                    //StartBuilding(SS3DServer.Singleton.playerManager.GetSessionByConnection(msg.SenderConnection).attachedAtom, 120, "Atom.Object.Worktop.Worktop", AlignmentOptions.AlignNone, false);
                    //StartBuilding(SS3DServer.Singleton.playerManager.GetSessionByConnection(msg.SenderConnection).attachedAtom, 120, "Tiles.Floor.Floor", AlignmentOptions.AlignTile, false);
                    //StartBuilding(SS3DServer.Singleton.playerManager.GetSessionByConnection(msg.SenderConnection).attachedAtom, 120, "Atom.Item.Container.Toolbox", AlignmentOptions.AlignSimilar, false);
                    StartBuilding(SS3DServer.Singleton.playerManager.GetSessionByConnection(msg.SenderConnection).attachedAtom, 120, "Atom.Item.Container.Toolbox", AlignmentOptions.AlignWall, false);
                    break;
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
            SS3DServer.Singleton.SendMessageTo(message, mob.attachedClient, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///  Cancels object placement mode for given mob.
        /// </summary>
        public void SendPlacementCancel(Atom.Atom mob)
        {
            NetOutgoingMessage message = SS3DNetServer.Singleton.CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.CancelPlacement);
            SS3DServer.Singleton.SendMessageTo(message, mob.attachedClient, NetDeliveryMethod.ReliableOrdered);
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
            newPermission.mobUid = mob.uid;
            newPermission.range = range;
            newPermission.type = objectType;
            newPermission.AlignOption = alignOption;
            newPermission.placeAnywhere = placeAnywhere;

            var mobPermissions = from BuildPermission permission in BuildPermissions
                                 where permission.mobUid == mob.uid
                                 select permission;

            if (mobPermissions.Count() > 0)
            {
                RevokeAllBuildPermissions(mob);
                SendPlacementCancel(mob);
            }
        }

        /// <summary>
        ///  Removes all building Permissions for given mob.
        /// </summary>
        public void RevokeAllBuildPermissions(Atom.Atom mob)
        {
            var mobPermissions = from BuildPermission permission in BuildPermissions
                                 where permission.mobUid == mob.uid
                                 select permission;

            if (mobPermissions.Count() > 0)
            {
                foreach (BuildPermission current in mobPermissions)
                {
                    BuildPermissions.Remove(current);
                }
            }
        }
    }
}
