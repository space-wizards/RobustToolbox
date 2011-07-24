using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;

namespace SS3d_server.Atom.Object.Door
{
    [Serializable()]
    public class Door : Object
    {

        DoorState status = DoorState.Closed;
        DoorState laststatus = DoorState.Closed;
        float openLength = 5000;
        float timeOpen = 0;

        public Door()
            : base()
        {
            name = "door";
        }

        protected override void ApplyAction(Atom a, Mob.Mob m)
        {
            laststatus = status;
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
                }
                
            }
            else if (status == DoorState.Open)
            {
                status = DoorState.Closed;
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

        }

        public override void Update(float framePeriod)
        {
            base.Update(framePeriod);

            if (status == DoorState.Closed)
            {
                updateRequired = false;
                timeOpen = 0;
            }
            else if (status == DoorState.Open)
            {
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
         }

        private void UpdateState()
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.Extended);
            message.Write((byte)status);
            SendMessageToAll(message);
        }

        public Door(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }
    }
}
