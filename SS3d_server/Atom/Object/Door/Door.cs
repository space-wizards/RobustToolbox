using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace SS3d_server.Atom.Object.Door
{
    public class Door : Object
    {

        bool open = false; // This should eventually be changed to an enum probably with the various states,
        // like open, closed, broken, emagged etc.

        float openLength = 5000;
        float timeOpen = 0;

        public Door()
            : base()
        {
            name = "door";
            
        }

        protected override void ApplyAction(Atom a, Mob.Mob m)
        {
            open = !open;
            timeOpen = 0;
            UpdateState();
            updateRequired = true;
        }

        public override void Update(float framePeriod)
        {
            base.Update(framePeriod);

            if (!open)
            {
                updateRequired = false;
                timeOpen = 0;
            }
            else
            {
                updateRequired = true;
                timeOpen += framePeriod;
                if (timeOpen > openLength)
                {
                    open = false;
                    UpdateState();
                }
            }
         }

        private void UpdateState()
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.Extended);
            message.Write(open);
            SendMessageToAll(message);
        }
    }
}
