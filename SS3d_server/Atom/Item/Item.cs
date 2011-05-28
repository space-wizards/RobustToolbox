using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using Lidgren.Network;

namespace SS3d_server.Atom.Item
{
    public class Item : Atom
    {
        public Item()
            : base()
        {

        }

        protected virtual void HandleExtendedMessage(NetIncomingMessage message)
        {
            ItemMessage i = (ItemMessage)message.ReadByte();
            switch (i)
            {
                default:
                    break;
            }
        }

        protected override void HandleClick(NetIncomingMessage message)
        {
        }
    }
}
