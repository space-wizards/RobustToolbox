using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;
using SS3D_shared.HelperClasses;
using Lidgren.Network;

namespace SS3D.Atom.Item
{
    public class Item : Atom
    {
        public Item()
            : base()
        {

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
    }
}
