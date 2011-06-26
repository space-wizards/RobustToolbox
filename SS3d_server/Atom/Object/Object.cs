using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using Lidgren.Network;
using SS3d_server.Atom.Mob;
using SS3d_server.Atom.Mob.HelperClasses;


namespace SS3d_server.Atom.Object
{
    public class Object : Atom
    {
        public Object()
            : base()
        {

        }

         public override void Update(float framePeriod)
        {
            base.Update(framePeriod);
        }

        protected override void HandleExtendedMessage(NetIncomingMessage message)
        {
            ItemMessage i = (ItemMessage)message.ReadByte();
            switch (i)
            {
                default:
                    break;
            }
        }

        protected override void ApplyAction(Atom a, Mob.Mob m)
        {

        }


    }
}
