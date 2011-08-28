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
            extensions.Add(new Extension.DummyExtension(this));
        }

        public override void SendState(NetConnection client)
        {
            base.SendState(client);

            SendAttachMessage();
        }

        public override void SerializedInit()
        {
            base.SerializedInit();
            rotation = 0f;
            // When things are created with reflection using serialization their default constructor
            // isn't called. Put things in here which need to be done when it's created.
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

        /// <summary>
        /// This is a method to apply an action to another atom. This may be confusing, but bear with me. The idea is
        /// to allow either the source or the target atom to execute actions. This code allows the target atom to execute code
        /// based on what has been used on it.
        /// </summary>
        /// <param name="a">The atom that has been used on this one.</param>
        /// <param name="m">The mob that has used atom a on this one.</param>
        protected override void ApplyAction(Atom a, Mob.Mob m)
        {
            if (a == null && holdingAppendage == null) //If mob's not holding an item and this item is not being held
            {
                PickedUpBy(m);
            }
            else if (a != null) // If mob's holding an item
                base.ApplyAction(a, m);

            //Otherwise do nothing.             
        }

        /// <summary>
        /// Bigtime method to apply actions. The reason this functionality is duplicated with ApplyActions is
        /// that when I write a new item, I don't want to write the code in two places. If I want to make a new atom and
        /// allow it to be affected by other atoms, I can write those actions just in that atom. If I want to write a new 
        /// item and allow it to affect other atoms, I can write that in that item and not everywhere else. 
        /// -spooge
        /// </summary>
        /// <param name="target">The atom that this one has been used on.</param>
        protected override void UsedOn(Atom target)
        {
            base.UsedOn(target);
            switch (target.GetType().ToString())
            {
                default:
                    //By default, Atoms will do damage.
                    target.Damage(damageAmount);
                    //Send attack animation (this is a retarded way of doing this really)
                    holdingAppendage.AnimateAttack();
                    break;
            }
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

            SendAttachMessage();
            SendAppendageUIUpdate(newHolder);
        }

        public virtual void SendAppendageUIUpdate(Mob.Mob target)
        {
            //send a message to the new holder's UI to put in the right image
            NetOutgoingMessage message = SS3DServer.Singleton.playerManager.GetSessionByConnection(target.attachedClient).CreateGuiMessage(SS3D_shared.GuiComponentType.AppendagesComponent);
            message.Write((byte)SS3D_shared.HandsComponentMessage.UpdateHandObjects);
            SS3DServer.Singleton.SendMessageTo(message, target.attachedClient);
        }
        

        /// <summary>
        /// Sends an attach message to the owner of the attached appendage
        /// </summary>
        public virtual void SendAttachMessage()
        {
            if (holdingAppendage == null)
                return;

            /// Ok, this will send a message to all clients saying that 
            /// this item is now attached to a certain appendage on the mob with id uid.
            NetOutgoingMessage outmessage = CreateAtomMessage();
            outmessage.Write((byte)AtomMessage.Extended);
            outmessage.Write((byte)ItemMessage.AttachTo);
            outmessage.Write(holdingAppendage.owner.uid);
            outmessage.Write(holdingAppendage.appendageName);
            SS3DServer.Singleton.SendMessageToAll(outmessage);
        }

        /// <summary>
        /// Called when a mob drops an item
        /// </summary>
        public virtual void Dropped()
        {
            Vector2 droppedposition = holdingAppendage.owner.position;
            float droppedrot = holdingAppendage.owner.rotation;
            Mob.Mob owner = holdingAppendage.owner;
            
            holdingAppendage.heldItem = null;
            holdingAppendage = null;

            NetOutgoingMessage outmessage = CreateAtomMessage();
            outmessage.Write((byte)AtomMessage.Extended);
            outmessage.Write((byte)ItemMessage.Detach);
            SS3DServer.Singleton.SendMessageToAll(outmessage);

            SendAppendageUIUpdate(owner);

            Translate(droppedposition, droppedrot);
        }

        public override void Push()
        {
            base.Push();

        }
    }
}
