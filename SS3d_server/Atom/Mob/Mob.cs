using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS3D_Server.Atom.Mob.HelperClasses;
using SS3D_shared.HelperClasses;
using SGO;

namespace SS3D_Server.Atom.Mob
{
    public class Mob : Atom
    {
        public float walkSpeed = 400.0f;
        public float runSpeed = 600.0f;
        public Item.Organs.BLOOD_TYPE blood_type = Item.Organs.BLOOD_TYPE.A; // Temporary
        public List<Item.Organs.Organ> organs = new List<Item.Organs.Organ>();

        public Dictionary<int, HelperClasses.Appendage> appendages;
        public Appendage selectedAppendage;

        public Dictionary<GUIBodyPart, Item.Item> equippedAtoms;

        public string animationState = "idle";

        public Mob()
            : base()
        {
            initAppendages();
            AddComponent(SS3D_shared.GO.ComponentFamily.Hands, ComponentFactory.Singleton.GetComponent("HumanHandsComponent"));
        }

        public override void Destruct()
        {
            base.Destruct();

            DropAllItems();
        }

        public override void SendState(NetConnection client)
        {
            base.SendState(client);

            foreach(GUIBodyPart part in equippedAtoms.Keys)
            {
                if (equippedAtoms[part] != null)
                {
                    SendEquipItem(equippedAtoms[part].Uid, part);
                }
            }

            if (IsDead())
                SendDeathMessage(client);
        }

        /// <summary>
        /// Initializes appendage dictionary
        /// </summary>
        protected virtual void initAppendages()
        {
            appendages = new Dictionary<int,Appendage>();
            appendages.Add(0, new HelperClasses.Appendage("LeftHand", 0, this));
            appendages.Add(1, new HelperClasses.Appendage("RightHand", 1, this));
            appendages[0].attackAnimation = "lattack";
            appendages[1].attackAnimation = "rattack";
            selectedAppendage = appendages[0];
        }

        protected override void HandleExtendedMessage(Lidgren.Network.NetIncomingMessage message)
        {
            MobMessage mobMessageType = (MobMessage)message.ReadByte();
            switch (mobMessageType)
            {
                case MobMessage.AnimationState:
                    HandleAnimationState(message);
                    break;
                case MobMessage.DropItem:
                    HandleDropItem();
                    break;
                case MobMessage.SelectAppendage:
                    SelectAppendage(message.ReadInt32());
                    break;
                case MobMessage.Equip:
                    EquipItem(message);
                    break;
                case MobMessage.Unequip:
                    UnequipItem(message);
                    break;
                default: 
                    break;
            }
        }

        protected virtual void HandleAnimationState(NetIncomingMessage message)
        {
            string state = message.ReadString();
            NetOutgoingMessage outmessage = CreateAtomMessage();
            outmessage.Write((byte)AtomMessage.Extended);
            outmessage.Write((byte)MobMessage.AnimationState);
            outmessage.Write(state);
            SendMessageToAll(outmessage);
        }

        /// <summary>
        /// Selects an appendage from the dictionary
        /// </summary>
        /// <param name="appendageName">name of appendage to select</param>
        public virtual void SelectAppendage(int appendageID)
        {
            if (appendages.Keys.Contains(appendageID))
                selectedAppendage = appendages[appendageID];
            SendSelectAppendage();
        }

        /// <summary>
        /// Sends a message to all clients telling them the mob has selected an appendage.
        /// </summary>
        public virtual void SendSelectAppendage()
        {
            NetOutgoingMessage outmessage = CreateAtomMessage();
            outmessage.Write((byte)AtomMessage.Extended);
            outmessage.Write((byte)MobMessage.SelectAppendage);
            outmessage.Write(selectedAppendage.ID);
            SendMessageToAll(outmessage);
        }


        /// <summary>
        /// Equips an item on a mob and then sends the result to everyone
        /// </summary>
        public virtual void EquipItem(NetIncomingMessage message)
        {
            int id = message.ReadInt32();
            GUIBodyPart part = (GUIBodyPart)message.ReadByte();
            if (equippedAtoms.ContainsKey(part) && equippedAtoms[part] == null)
            {
                Atom atom = atomManager.GetAtom(id);
                if (atom.IsChildOfType(typeof(Item.Item)))
                {
                    equippedAtoms[part] = (Item.Item)atom;
                    SendEquipItem(atom.Uid, part);

                    if (equippedAtoms[part].holdingAppendage != null)
                    {
                        equippedAtoms[part].SendDetatchMessage();
                        equippedAtoms[part].holdingAppendage.heldItem = null;
                        equippedAtoms[part].holdingAppendage = null;
                    }
                }
            }
        }

        /// <summary>
        /// Equips an item on a mob and then sends the result to everyone
        /// </summary>
        public virtual void EquipItem(int id, GUIBodyPart targetPart)
        {
            if (equippedAtoms.ContainsKey(targetPart) && equippedAtoms[targetPart] == null)
            {
                Atom atom = atomManager.GetAtom(id);
                if (atom.IsChildOfType(typeof(Item.Item)))
                {
                    equippedAtoms[targetPart] = (Item.Item)atom;
                    SendEquipItem(atom.Uid, targetPart);

                    if (equippedAtoms[targetPart].holdingAppendage != null)
                    {
                        equippedAtoms[targetPart].SendDetatchMessage();
                        equippedAtoms[targetPart].holdingAppendage.heldItem = null;
                        equippedAtoms[targetPart].holdingAppendage = null;
                    }
                }
            }
        }

        /// <summary>
        /// Sends a message to everyone that a mob just equipped an item
        /// </summary>
        public virtual void SendEquipItem(int id, GUIBodyPart part)
        {
            NetOutgoingMessage outmessage = CreateAtomMessage();
            outmessage.Write((byte)AtomMessage.Extended);
            outmessage.Write((byte)MobMessage.Equip);
            outmessage.Write(id);
            outmessage.Write((byte)part);
            SendMessageToAll(outmessage);
        }

        /// <summary>
        /// Unequips an item from a mob, then sends the result to everyone
        /// </summary>
        public virtual void UnequipItem(NetIncomingMessage message)
        {
            GUIBodyPart part = (GUIBodyPart)message.ReadByte();
            if (equippedAtoms.ContainsKey(part) &&
                equippedAtoms[part] != null &&
                selectedAppendage.heldItem == null)
            {
                SendUnequipItem(part);
                equippedAtoms[part].PickedUpBy(this);
                equippedAtoms[part] = null;
                
            }
        }

        /// <summary>
        /// Sends a message to everyone saying a mob just unequipped an item
        /// </summary>
        public virtual void SendUnequipItem(GUIBodyPart part)
        {
            NetOutgoingMessage outmessage = CreateAtomMessage();
            outmessage.Write((byte)AtomMessage.Extended);
            outmessage.Write((byte)MobMessage.Unequip);
            outmessage.Write((byte)part);
            SendMessageToAll(outmessage);
        }

        public virtual void HandleDropItem()
        {
            if (selectedAppendage.heldItem != null)
                selectedAppendage.heldItem.Dropped();
        }

        public virtual void DropAllItems()
        {
            foreach (Appendage a in appendages.Values)
            {
                if (a.heldItem != null)
                    a.heldItem.Dropped();
            }
        }

        public virtual void AnimateOnce(string animation)
        {
            NetOutgoingMessage outmessage = CreateAtomMessage();
            outmessage.Write((byte)AtomMessage.Extended);
            outmessage.Write((byte)MobMessage.AnimateOnce);
            outmessage.Write(animation);
            SendMessageToAll(outmessage);
        }

        public override void Die()
        {
            base.Die();

            DropAllItems();
            SendDeathMessage();
            SetSpriteState(9); //Set to dead sprite lol
            
            rotation = 90;
            SendInterpolationPacket(true);
            //AnimateOnce("death");
        }

        public virtual void SendDeathMessage()
        {
            NetOutgoingMessage msg = CreateAtomMessage();
            msg.Write((byte)AtomMessage.Extended);
            msg.Write((byte)MobMessage.Death);
            SendMessageToAll(msg, NetDeliveryMethod.ReliableOrdered);
        }
        
        public virtual void SendDeathMessage(NetConnection client)
        {
            NetOutgoingMessage msg = CreateAtomMessage();
            msg.Write((byte)AtomMessage.Extended);
            msg.Write((byte)MobMessage.Death);
            SendMessageTo(msg, client, NetDeliveryMethod.ReliableOrdered);
        }

        public override void Damage(int amount, int damagerId)
        {
            base.Damage(amount, damagerId);

            if (IsDead())
                Die();
        }
    }
}
