using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;
using SS3D_shared.HelperClasses;
using Lidgren.Network;
using SS3D.Atom.Mob.HelperClasses;

namespace SS3D.Atom.Item
{
    public class Item : Atom
    {
        public Mogre.Vector3 heldOffset = Mogre.Vector3.ZERO;           // the offset vector when held
        public Mogre.Quaternion heldQuat = Mogre.Quaternion.IDENTITY;   // the rotation when held
        

        public Appendage holdingAppendage = null;
                public Item()
            : base()
        {

        }

        protected override void HandleExtendedMessage(NetIncomingMessage message)
        {
            ItemMessage i = (ItemMessage)message.ReadByte();
            switch (i)
            {
                case ItemMessage.AttachTo:
                    HandleAttachTo(message);
                    break;
                case ItemMessage.Detach:
                    HandleDetach();
                    break;
                default:
                    break;
            }
        }

        protected virtual void HandleAttachTo(NetIncomingMessage message)
        {
            ushort holderuid = message.ReadUInt16();
            string appendagename = message.ReadString();
            Mob.Mob m = (Mob.Mob)atomManager.GetAtom(holderuid);
            Appendage a = m.appendages[appendagename];
            if (a == null)
                return; //TODO ERROR FUCK

            AttachTo(m, a);
        }

        protected virtual void HandleDetach()
        {
            if(holdingAppendage != null)
            {
                holdingAppendage.owner.Entity.DetachObjectFromBone(Entity);

                Mogre.Vector3 mobpos = holdingAppendage.owner.position;
                position = mobpos;
                Node.Position = position + offset;
                Node.AttachObject(Entity);
                holdingAppendage = null;
            }
        }

        protected virtual void AttachTo(Mob.Mob m, Appendage a)
        {
            Entity.DetachFromParent();

            m.Entity.AttachObjectToBone(a.bone, Entity, heldQuat, heldOffset);
            holdingAppendage = a;
        }
    }
}
