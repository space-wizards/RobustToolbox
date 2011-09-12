using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using Lidgren.Network;
using SS3D.Atom.Mob.HelperClasses;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace SS3D.Atom.Item
{
    public abstract class Item : Atom
    {
        public Appendage holdingAppendage = null;
                public Item()
            : base()
        {

        }

        public override void Draw()
        {
            base.Draw();

            if (spriteNames[-1] != null)
            {
                string baseName = spriteNames[-1];
                SetSpriteName(0, baseName + "_front");
                SetSpriteName(1, baseName + "_side");
                SetSpriteName(2, baseName + "_back");
                SetSpriteName(3, baseName + "_side");
                SetSpriteName(5, baseName + "_inhand");
            }
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
                    HandleDetatch();
                    break;
                case ItemMessage.DropItem:
                    HandleDrop();
                    break;
                default:
                    break;
            }
        }

        protected virtual void HandleAttachTo(NetIncomingMessage message)
        {
            ushort holderuid = message.ReadUInt16();
            int appendageID = message.ReadInt32();
            Mob.Mob m = (Mob.Mob)atomManager.GetAtom(holderuid);
            Appendage a = m.appendages[appendageID];
            if (a == null)
                return; //TODO ERROR FUCK

            AttachTo(m, a);
        }

        protected virtual void HandleDrop()
        {
            if (holdingAppendage != null)
            {
                Vector2D mobpos = holdingAppendage.owner.position;
                position = mobpos;
            }
            holdingAppendage.attachedItem = null;
            holdingAppendage = null;
            visible = true;
            SetSpriteByIndex(-1);
        }

        public virtual void HandleDetatch()
        {
            if (holdingAppendage != null)
            {
                holdingAppendage.attachedItem = null;
                holdingAppendage = null;
            }
        }

        protected virtual void AttachTo(Mob.Mob m, Appendage a)
        {
            this.visible = false;
            holdingAppendage = a;
            a.attachedItem = this;
        }
    }
}
