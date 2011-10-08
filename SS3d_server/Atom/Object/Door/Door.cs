using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;
using SGO;

namespace SS3D_Server.Atom.Object.Door
{
    [Serializable()]
    public class Door : Object
    {

        /*DoorState status = DoorState.Closed;
        DoorState laststatus = DoorState.Closed;
        float openLength = 5000;
        float timeOpen = 0;
        */
        public Door()
            : base()
        {
            name = "door";
            AddComponent(SS3D_shared.GO.ComponentFamily.Interactable, ComponentFactory.Singleton.GetComponent("BasicInteractableComponent"));
            AddComponent(SS3D_shared.GO.ComponentFamily.Collidable, ComponentFactory.Singleton.GetComponent("CollidableComponent"));
            AddComponent(SS3D_shared.GO.ComponentFamily.LargeObject, ComponentFactory.Singleton.GetComponent("BasicDoorComponent"));
            GetComponent(SS3D_shared.GO.ComponentFamily.LargeObject).SetParameter(new ComponentParameter("OpenSprite", "string", "door_ewo"));
            GetComponent(SS3D_shared.GO.ComponentFamily.LargeObject).SetParameter(new ComponentParameter("ClosedSprite", "string", "door_ew"));
        }

        protected override void ApplyAction(Atom a, Mob.Mob m)
        {
            /*laststatus = status;
            if (status == DoorState.Closed)
            {
                // Just as a test of item interaction, lets make a crowbar break / fix a door.
                if (a != null && a.IsTypeOf(typeof(Item.Tool.Crowbar)))
                {
                    status = DoorState.Broken;
                }
                else
                {
                    status = DoorState.Open;
                    SS3DServer.Singleton.chatManager.SendChatMessage(ChatChannel.Default, m.name + " opened the " + name + ".", "", m.Uid);
                }
                
            }
            else if (status == DoorState.Open)
            {
                status = DoorState.Closed;
                SS3DServer.Singleton.chatManager.SendChatMessage(ChatChannel.Default, m.name + " closed the " + name + ".", "", m.Uid);
            }
            else if (status == DoorState.Broken)
            {
                if (a != null && a.IsTypeOf(typeof(Item.Tool.Crowbar)))
                {
                    status = DoorState.Open;
                }
            }
            // If the status hasn't changed, we don't need to send anything.
            if (laststatus != status)
            {
                timeOpen = 0;
                UpdateState();
                updateRequired = true;
            }
            */
        }

        /*public override void Update(float framePeriod)
        {
            base.Update(framePeriod);

            //Make closed doors block gas
            var occupiedTilePos = SS3DServer.Singleton.map.GetTileArrayPositionFromWorldPosition(position);
            var occupiedTile = SS3DServer.Singleton.map.GetTileAt(occupiedTilePos.X, occupiedTilePos.Y);

            if (status == DoorState.Closed)
            {
                occupiedTile.gasPermeable = false;
                occupiedTile.gasCell.blocking = true;
                updateRequired = false;
                timeOpen = 0;
            }
            else if (status == DoorState.Open)
            {
                occupiedTile.gasPermeable = true;
                occupiedTile.gasCell.blocking = false;
                updateRequired = true;
                timeOpen += framePeriod;
                if (timeOpen > openLength)
                {
                    status = DoorState.Closed;
                    UpdateState();
                }
            }
            else if (status == DoorState.Broken)
            {
                updateRequired = false;
                timeOpen = 0;
            }
         }*/

        /*private void UpdateState()
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.Extended);
            message.Write((byte)status);
            SendMessageToAll(message);
        }*/

        public Door(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
            AddComponent(SS3D_shared.GO.ComponentFamily.Interactable, ComponentFactory.Singleton.GetComponent("BasicInteractableComponent"));
            AddComponent(SS3D_shared.GO.ComponentFamily.Collidable, ComponentFactory.Singleton.GetComponent("CollidableComponent"));
            AddComponent(SS3D_shared.GO.ComponentFamily.LargeObject, ComponentFactory.Singleton.GetComponent("BasicDoorComponent"));
            GetComponent(SS3D_shared.GO.ComponentFamily.LargeObject).SetParameter(new ComponentParameter("OpenSprite", "string", "door_ewo"));
            GetComponent(SS3D_shared.GO.ComponentFamily.LargeObject).SetParameter(new ComponentParameter("ClosedSprite", "string", "door_ew"));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }
    }
}
