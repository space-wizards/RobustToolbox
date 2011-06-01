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
        public bool attachedAppendage;


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

        /// <summary>
        /// This is called when a mob picks up an item.
        /// </summary>
        /// <param name="uid">an atom uid that has just picked up this item</param>
        public virtual void PickedUpBy(ushort uid)
        {
            
        }

        public virtual void Dropped(ushort uid)
        {

        }
    }
}
