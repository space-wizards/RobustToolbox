using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using Lidgren.Network;

using SS3D_shared;

namespace SS3D.Modules.UI.Components
{
    public class HumanHandsGui : GuiComponent
    {
        public override Point Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                lSprite.Position = position;
                rSprite.Position = new Point(position.X + 67, position.Y);
            }
        }

        private Sprite lSprite;
        private int lAppendageID = 0;
        private Sprite rSprite;
        private int rAppendageID = 1;
        private Sprite lObjectSprite;
        private Sprite rObjectSprite;
        private Sprite backgroundSprite;
        private Color inactiveColor;

        private bool lActive = true;
        private bool rActive = false;

        public HumanHandsGui(PlayerController _playerController)
            : base(_playerController)
        {
            lSprite = ResMgr.Singleton.GetSprite("l_hand");
            rSprite = ResMgr.Singleton.GetSprite("r_hand");
            backgroundSprite = ResMgr.Singleton.GetSprite("1pxwhite");
            inactiveColor = Color.FromArgb(35, 35, 35);
        }

        public void ChangeLeftAppendageID(int newID)
        {
            Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;
            if (m.appendages.ContainsKey(newID))
                lAppendageID = newID;
        }

        public void ChangeRightAppendageID(int newID)
        {
            Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;
            if (m.appendages.ContainsKey(newID))
                rAppendageID = newID;
        }

        public void ActivateLeft()
        {
            lActive = true;
            rActive = false;
            Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;
            m.SendSelectAppendage(lAppendageID);
        }

        public void ActivateRight()
        {
            lActive = false;
            rActive = true;
            Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;
            m.SendSelectAppendage(rAppendageID);
        }

        public int GetSelectedAppendage()
        {
            if (lActive)
                return lAppendageID;
            else
                return rAppendageID;
        }

        private void UpdateAppendageObjects()
        {
            if (playerController.controlledAtom == null)
                return;

            var mob = (Atom.Mob.Mob)playerController.controlledAtom;
            if (mob.appendages[0].attachedItem != null)
                lObjectSprite = ResMgr.Singleton.GetSprite(mob.appendages[lAppendageID].attachedItem.spritename);
            else
                lObjectSprite = null;

            if (mob.appendages[1].attachedItem != null)
                rObjectSprite = ResMgr.Singleton.GetSprite(mob.appendages[rAppendageID].attachedItem.spritename);
            else
                rObjectSprite = null;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (e.Key == KeyboardKeys.Tab)
            {
                SwitchHands();
                return true;
            }
            return false;
        }

        private void SwitchHands()
        {
            if (lActive)
                ActivateRight();
            else
                ActivateLeft();
        }

        public override void Render()
        {
            backgroundSprite.Position = lSprite.Position;
            backgroundSprite.Size = new Vector2D(lSprite.Position.X - (rSprite.Position.X + rSprite.Width), lSprite.Height);
            backgroundSprite.Color = System.Drawing.Color.FromArgb(51, 56, 64);
            backgroundSprite.Opacity = 240;
            backgroundSprite.Draw();

            if (lActive)
            {
                lSprite.Color = Color.White;
                lSprite.Draw();
            }
            else
            {
                lSprite.Color = inactiveColor;
                lSprite.Draw();
            }

            if (rActive)
            {
                rSprite.Color = Color.White;
                rSprite.Draw();
            }
            else
            {
                rSprite.Color = inactiveColor;
                rSprite.Draw();
            }

            if (playerController.controlledAtom == null)
                return;
            var mob = (Atom.Mob.Mob)playerController.controlledAtom;
            if (mob.appendages[0].attachedItem != null)
            {
                mob.appendages[0].attachedItem.sprite.SetPosition(lSprite.Position.X + 40, lSprite.Position.Y + 25);
                mob.appendages[0].attachedItem.sprite.Color = System.Drawing.Color.White;
                mob.appendages[0].attachedItem.sprite.Rotation = 0f;
                float factor = Math.Min(50f / mob.appendages[0].attachedItem.sprite.Width, 50f / mob.appendages[0].attachedItem.sprite.Height);
                mob.appendages[0].attachedItem.sprite.UniformScale = factor;
                mob.appendages[0].attachedItem.sprite.Draw();
                mob.appendages[0].attachedItem.sprite.UniformScale = 1f;
            }
            if (mob.appendages[1].attachedItem != null)
            {
                mob.appendages[1].attachedItem.sprite.SetPosition(rSprite.Position.X + 60, rSprite.Position.Y + 25);
                mob.appendages[1].attachedItem.sprite.Color = System.Drawing.Color.White;
                mob.appendages[1].attachedItem.sprite.Rotation = 0f;
                float factor = Math.Min(50f / mob.appendages[1].attachedItem.sprite.Width, 50f / mob.appendages[1].attachedItem.sprite.Height);
                mob.appendages[1].attachedItem.sprite.UniformScale = factor;
                mob.appendages[1].attachedItem.sprite.Draw();
                mob.appendages[1].attachedItem.sprite.UniformScale = 1f;
            }
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
            HandsComponentMessage messageType = (HandsComponentMessage)message.ReadByte();
            switch (messageType)
            {
                case HandsComponentMessage.UpdateHandObjects:
                    HandleUpdateHandObjects();
                    break;
                default: break;
            }
        }

        private void HandleUpdateHandObjects()
        {
            UpdateAppendageObjects();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            System.Drawing.RectangleF mouseAABB = new System.Drawing.RectangleF(e.Position.X, e.Position.Y, 1, 1);
            if(mouseAABB.IntersectsWith(lSprite.AABB))
            {
                if (!lActive)
                    ActivateLeft();
                return true;
            }
            else if(mouseAABB.IntersectsWith(rSprite.AABB))
            {
                if (!rActive)
                    ActivateRight();
                return true;
            }
            else
            {
                return false;
            }
        }

        
    }
}
