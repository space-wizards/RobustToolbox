using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;

namespace SS3D_Server.Atom.Object.Worktop
{
    [Serializable()]
    public class Worktop : Object
    {
        private Vector2 placementVariance = new Vector2(16, 16);

        public Worktop()
            : base()
        {
            name = "worktop";
        }

        protected override void ApplyAction(Atom a, Mob.Mob m)
        {
            Vector2 newPos = this.position;

            Random rnd = new Random();
            int rndVarX = rnd.Next(-(int)placementVariance.X, (int)placementVariance.X); //Customize this depending on the type of sprite.
            int rndVarY = rnd.Next(-(int)placementVariance.Y, 0);
            newPos += new Vector2(rndVarX, rndVarY);

            float newRot = 0;

            Item.Item usedItem = (Item.Item)a; //This will go to shit if someone puts a vent on the table.
            usedItem.holdingAppendage.heldItem = null;
            usedItem.holdingAppendage = null;

            NetOutgoingMessage detachMsg = usedItem.CreateAtomMessage();
            detachMsg.Write((byte)AtomMessage.Extended);
            detachMsg.Write((byte)ItemMessage.Detach);
            SS3DServer.Singleton.SendMessageToAll(detachMsg);

            atomManager.SetDrawDepthAtom(a, 1); //base is 0 - draw this above the table. This sets the value and sends it to all clients.

            usedItem.SendAppendageUIUpdate(m);

            usedItem.Translate(newPos, newRot);
        }

        public override void Update(float framePeriod)
        {
            base.Update(framePeriod);
        }

        public Worktop(SerializationInfo info, StreamingContext ctxt)
        {
            //SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            //base.GetObjectData(info, ctxt);
        }
    }
}