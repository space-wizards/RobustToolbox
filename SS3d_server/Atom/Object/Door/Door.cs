using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace SS3d_server.Atom.Object.Door
{
    public class Door : Object
    {

        DoorState status = DoorState.Closed;
        float openLength = 5000;
        float timeOpen = 0;

        public Door()
            : base()
        {
            name = "door";
            
        }

        protected override void ApplyAction(Atom a, Mob.Mob m)
        {
            if (status != DoorState.Broken)
            {
                if (status == DoorState.Closed)
                {
                    status = DoorState.Open;
                }
                else
                {
                    status = DoorState.Closed;
                }
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

            }
         }

        private void UpdateState()
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.Extended);
            message.Write((byte)status);
            SendMessageToAll(message);
        }
    }
}
