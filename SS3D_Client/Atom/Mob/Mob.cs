using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS3D.Atom.Mob.HelperClasses;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using CGO;
using SS3D_shared.GO;

namespace SS3D.Atom.Mob
{
    public abstract class Mob : Atom
    {
        
        // TODO Make these some sort of well-organized global constant
        public float walkSpeed = 400.0f;
        public float runSpeed = 600.0f;

        public bool isDead = false;

        public Dictionary<int, HelperClasses.Appendage> appendages;
        public Appendage selectedAppendage;

        public Dictionary<GUIBodyPart, Item.Item> equippedAtoms;

        public Mob()
            : base()
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            AddComponent(SS3D_shared.GO.ComponentFamily.Hands, ComponentFactory.Singleton.GetComponent("HumanHandsComponent"));
            SpriteComponent c = (SpriteComponent)ComponentFactory.Singleton.GetComponent("MobSpriteComponent");
            c.SetParameter(new ComponentParameter("basename", typeof(string), "human"));
            AddComponent(SS3D_shared.GO.ComponentFamily.Renderable, c);
            AddComponent(ComponentFamily.Equipment, ComponentFactory.Singleton.GetComponent("HumanEquipmentComponent"));

            //speed = walkSpeed;
        }

        public virtual void initAppendages()
        {
            appendages = new Dictionary<int, Appendage>();
            appendages.Add(0, new Appendage("Bip001 L Hand", "LeftHand", 0, this));
            appendages.Add(1, new Appendage("Bip001 R Hand", "RightHand", 1, this));
            selectedAppendage = appendages[0];
        }

        public virtual Item.Item GetItemOnAppendage(int appendageID)
        {
            if (!appendages.ContainsKey(appendageID)) return null;
            if (appendages[appendageID] == null) return null;
            if (appendages[appendageID].attachedItem == null) return null;
            else return appendages[appendageID].attachedItem;
        }

        public override void SetUp(int _uid, AtomManager _atomManager)
        {
            base.SetUp(_uid, _atomManager);

            //sprite.UniformScale = 1f;
            initAppendages();

            equippedAtoms = new Dictionary<GUIBodyPart, Item.Item>();
        }

        /// <summary>
        /// Base atoms dont have animations, fuckface
        /// </summary>
        public override void Draw()
        {
            base.Draw();
        }

        public override void initKeys()
        {
            base.initKeys();
        }

        public override void Update(float time)
        {
            base.Update(time);



            //Update every tick
            updateRequired = true;
        }

        /// <summary>
        /// Override to handle walk animations
        /// </summary>
        public override void UpdatePosition()
        {
            /*Vector2D lastPosition = position;
            base.UpdatePosition();
            UpdateCharacterDirection(Position - lastPosition);
            
            foreach (Appendage a in appendages.Values)
            {
                if (a.attachedItem != null)
                {
                    a.attachedItem.UpdatePosition();
                }
            }

            if (isDead)
                return;
             */
        }

        public virtual void UpdateCharacterDirection(Vector2D movementVector)
        {
            /*float angle = movementVector.Angle();
            if (movementVector == Vector2D.Zero)
                return;

            if (angle > -0.25f * Math.PI && angle < 0.25f * Math.PI)
            {
                //SetSpriteByIndex(3);
            }
            else if (angle > 0.25f * Math.PI && angle < 0.75f * Math.PI)
            {
                //SetSpriteByIndex(0);
            }
            else if(angle < -0.25f * Math.PI && angle > -0.75f * Math.PI)
            {
                //SetSpriteByIndex(2);
            }
            else
            {
                //SetSpriteByIndex(1);
            }
            

             */
        }

        public virtual void HandleKC_Q(bool state)
        {
            if (state == true)
                return;
            else
                SendDropItem();
        }

        protected override void HandleExtendedMessage(NetIncomingMessage message)
        {
            MobMessage mobMessageType = (MobMessage)message.ReadByte();
            switch (mobMessageType)
            {
                case MobMessage.SelectAppendage:
                    HandleSelectAppendage(message);
                    break;
                case MobMessage.Death:
                    HandleDeath();
                    break;
                case MobMessage.Equip:
                    HandleEquipItem(message);
                    break;
                case MobMessage.Unequip:
                    HandleUnEquipItem(message);
                    break;
                default: break;
            }
        }

        private void HandleDeath()
        {
            isDead = true;

            //Clear key handlers
            /*
            keyHandlers.Clear();
            keyStates.Clear();*/
        }
        
        /// <summary>
        /// Sets selected appendage to what is contained in the message
        /// </summary>
        /// <param name="message">Incoming netmessage</param>
        protected virtual void HandleSelectAppendage(NetIncomingMessage message)
        {
            SetSelectedAppendage(message.ReadInt32());
        }

        /// <summary>
        /// Sets selected appendage to the appendage named
        /// </summary>
        /// <param name="appendageName">Appendage name</param>
        protected virtual void SetSelectedAppendage(int appendageID)
        {
            if (appendages.Keys.Contains(appendageID))
                selectedAppendage = appendages[appendageID];
        }

        public virtual void SendSelectAppendage(int appendageID)
        {
            if (!appendages.ContainsKey(appendageID))
                return;

            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.Extended);
            message.Write((byte)MobMessage.SelectAppendage);
            message.Write(appendageID);
            SendMessage(message);
        }

        /// <summary>
        /// Sends a message to drop the item in the currently selected appendage
        /// </summary>
        protected virtual void SendDropItem()
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.Extended);
            message.Write((byte)MobMessage.DropItem);
            SendMessage(message);
        }

        /// <summary>
        /// Sends a message saying we want to equip an item
        /// </summary>
        public virtual void SendEquipItem(Item.Item item, GUIBodyPart part)
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.Extended);
            message.Write((byte)MobMessage.Equip);
            message.Write(item.Uid);
            message.Write((byte)part);
            SendMessage(message);
        }

        /// <summary>
        /// Sends a message saying we want to Unequip an item
        /// </summary>
        public virtual void SendUnequipItem(GUIBodyPart part)
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.Extended);
            message.Write((byte)MobMessage.Unequip);
            message.Write((byte)part);
            SendMessage(message);
        }

        /// <summary>
        /// Equips an item on the appropriate body part
        /// </summary>
        public virtual void HandleEquipItem(NetIncomingMessage message)
        {
            int id = message.ReadInt32();
            GUIBodyPart part = (GUIBodyPart)message.ReadByte();

            if (!equippedAtoms.ContainsKey(part))
            {
                equippedAtoms.Add(part, null);
            }
            equippedAtoms[part] = (Item.Item)atomManager.GetAtom(id);
            equippedAtoms[part].SendMessage(null, ComponentMessageType.ItemWorn, null);
            equippedAtoms[part].visible = false;
        }

        /// <summary>
        /// Unequips an item from the appropriate body part
        /// </summary>
        public virtual void HandleUnEquipItem(NetIncomingMessage message)
        {
            GUIBodyPart part = (GUIBodyPart)message.ReadByte();

            if (!equippedAtoms.ContainsKey(part))
                return;

            equippedAtoms[part].SendMessage(null, ComponentMessageType.ItemUnWorn, null);
            equippedAtoms[part] = null;
        }

        /// <summary>
        /// Gets the atom on the passed in body part
        /// </summary>
        public virtual Atom GetEquippedAtom(GUIBodyPart part)
        {
            if (equippedAtoms.ContainsKey(part))
                return equippedAtoms[part];
            return null;
        }

        public override void Render(float xTopLeft, float yTopLeft)
        {
            /*if (GetSpriteIndex() == 3)
            {
                //sprite.HorizontalFlip = true;
            }
            base.Render(xTopLeft, yTopLeft);
            if (GetSpriteIndex() == 3)
            {
                //sprite.HorizontalFlip = false;
            }
            // Lets draw all their inventory
            foreach (Atom atom in equippedAtoms.Values)
            {
                if (atom != null)
                {
                    //atom.SetSpriteByIndex(GetSpriteIndex()); // Set the index to the same as the mob so it draws the correct direction
                    //atom.sprite.Position = sprite.Position;
                    //atom.sprite.Color = System.Drawing.Color.FromArgb(255, sprite.Color);
                    if (GetSpriteIndex() == 3)
                    {
                    //    atom.sprite.HorizontalFlip = true;
                    }
                    //atom.sprite.Draw();
                    //if (GetSpriteIndex() == 3)
                    {
                    //    atom.sprite.HorizontalFlip = false;
                    }
                    //atom.SetSpriteByIndex(-1); // Reset the index to the on map value for the GUI and in case it's dropped
                }
            }
            
            // Lets draw their appendages
            foreach (Appendage a in appendages.Values)
            {
                if (a.attachedItem != null)
                {
                    //a.attachedItem.SetSpriteByIndex(5);
                    //if (a.attachedItem.sprite.Image.Name == "noSprite")
                        //a.attachedItem.SetSpriteByIndex(-1);
                    //a.attachedItem.sprite.Position = sprite.Position + a.GetHoldPosition(GetSpriteIndex());
                    if (GetSpriteIndex() == 3)
                    {
                       // a.attachedItem.sprite.HorizontalFlip = true;
                    }
                    //a.attachedItem.sprite.Draw();
                    if (GetSpriteIndex() == 3)
                    {
                     //   a.attachedItem.sprite.HorizontalFlip = false;
                    }
                    //a.attachedItem.SetSpriteByIndex(-1);
                }
            }
            */
        }
    }
}
