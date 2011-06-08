using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using Lidgren.Network;
using SS3d_server.Atom.Mob;
using SS3d_server.Atom.Mob.HelperClasses;

namespace SS3d_server.Atom.Item
{
    public class Item : Atom
    {
        public Appendage holdingAppendage = null;
        private float fallSpeed = 5.0f;

        public bool isWeapon = true; // By default every holdable object is usable as a weapon.
        public int damageAmount = 10; // By default each hit with an item causes 10 damage.

        public Item()
            : base()
        {

        }

        public override void Update()
        {
            base.Update();
            
            double heightAboveTile = position.Y - (atomManager.netServer.map.GetHeightAboveTileAt(position) + 1.0f);
            if (heightAboveTile > 1.0)
            {
                if (heightAboveTile > fallSpeed)
                {
                    MoveTo(position - new Vector3(0, fallSpeed, 0));
                }
                else
                {
                    MoveTo(position - new Vector3(0,heightAboveTile,0));
                }
                updateRequired = true;
            }
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
            if (a == null)
                PickedUpBy(m);
            else
                base.ApplyAction(a, m);
        }

        /// <summary>
        /// This is called when a mob picks up an item.
        /// </summary>
        /// <param name="uid">an atom uid that has just picked up this item</param>
        public virtual void PickedUpBy(Mob.Mob newHolder)
        {
            //We store the appendage that is holding this item
            holdingAppendage = newHolder.selectedAppendage;
            //The appendage stores the item it is holding. This is probably redundant, but it is convenient.
            newHolder.selectedAppendage.heldItem = this;

            /// Ok, this will send a message to all clients saying that 
            /// this item is now attached to a certain appendage on the mob with id uid.
            NetOutgoingMessage outmessage = CreateAtomMessage();
            outmessage.Write((byte)AtomMessage.Extended);
            outmessage.Write((byte)ItemMessage.AttachTo);
            outmessage.Write(newHolder.uid);
            outmessage.Write(holdingAppendage.appendageName);
            atomManager.netServer.SendMessageToAll(outmessage);
        }

        /// <summary>
        /// Called when a mob drops an item
        /// </summary>
        /// <param name="uid">mob that dropped the item.</param>
        public virtual void Dropped(ushort uid)
        {

        }

        /// <summary>
        /// Bigtime method to apply actions. The reason this functionality is duplicated with ApplyActions is
        /// that when I write a new item, I don't want to write the code in two places. If I want to make a new atom and
        /// allow it to be affected by other atoms, I can write those actions just in that atom. If I want to write a new 
        /// item and allow it to affect other atoms, I can write that in that item and not everywhere else. 
        /// -spooge
        /// </summary>
        /// <param name="target"></param>
        protected override void UsedOn(Atom target)
        {
            switch (target.GetType().ToString())
            {
                default:
                    //By default, Atoms will do damage.
                    target.Damage(damageAmount);
                    break;
            }
        }

        public override void Push()
        {
            base.Push();

        }
    }
}
