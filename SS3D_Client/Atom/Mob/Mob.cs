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

        protected override void HandleExtendedMessage(NetIncomingMessage message)
        {
            MobMessage mobMessageType = (MobMessage)message.ReadByte();
            switch (mobMessageType)
            {
                case MobMessage.Death:
                    HandleDeath();
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
