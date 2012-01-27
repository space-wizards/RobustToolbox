using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using GorgonLibrary;
using GorgonLibrary.Framework;
using GorgonLibrary.GUI;
using GorgonLibrary.Graphics;
using GorgonLibrary.Graphics.Utilities;
using GorgonLibrary.InputDevices;
using ClientResourceManager;
using CGO;
using SS3D.Modules;
using SS3D;

namespace SS3D.UserInterface
{
    public class GuiItemSlot : GuiComponent
    {
        private EquipmentSlot bodyPart; // The bodypart we reference
        private Type atomType; // The type of atoms we can accept
        private Sprite slot;
        private Label text;
        private bool highlight = false;
        private Vector2D outlinePos = new Vector2D(1078, 632); // TODO: Remove magic numbers
        private Sprite outline;

        public GuiItemSlot(PlayerController _playerController, EquipmentSlot _bodyPart)
            : base(_playerController)
        {
            bodyPart = _bodyPart;
            slot = ResMgr.Singleton.GetSprite("slot");
            outline = ResMgr.Singleton.GetSprite("GUI_" + bodyPart);
            text = new Label(bodyPart.ToString());
            position = new Point(0, 12);
            SetAtomType();
        }

        public void SetAtomType()
        {
            switch (bodyPart)
            {
                case EquipmentSlot.Feet:
                    //atomType = typeof(Atom.Item.Wearable.Feet.Feet);
                    break;
                case EquipmentSlot.Inner:
                    //atomType = typeof(Atom.Item.Wearable.Inner.Inner);
                    break;
                case EquipmentSlot.Ears:
                    //atomType = typeof(Atom.Item.Wearable.Ears.Ears);
                    break;
                case EquipmentSlot.Eyes:
                    //atomType = typeof(Atom.Item.Wearable.Eyes.Eyes);
                    break;
                case EquipmentSlot.Hands:
                    //atomType = typeof(Atom.Item.Wearable.Hands.Hands);
                    break;
                case EquipmentSlot.Head:
                    //atomType = typeof(Atom.Item.Wearable.Head.Head);
                    break;
                case EquipmentSlot.Mask:
                    //atomType = typeof(Atom.Item.Wearable.Mask.Mask);
                    break;
                case EquipmentSlot.Outer:
                    //atomType = typeof(Atom.Item.Wearable.Outer.Outer);
                    break;
                case EquipmentSlot.Belt:
                    //atomType = typeof(Atom.Item.Wearable.Belt.Belt);
                    break;
                case EquipmentSlot.Back:
                case EquipmentSlot.None:
                default:
                    atomType = typeof(Entity);
                    break;
            }
        }

        public void SetOutlinePosition(Vector2D pos)
        {
            outlinePos = pos;
        }

        public EquipmentSlot GetBodyPart()
        {
            return bodyPart;
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            slot.Position = position;
            System.Drawing.RectangleF mouseAABB = new System.Drawing.RectangleF(e.Position.X, e.Position.Y, 1, 1);
            if (mouseAABB.IntersectsWith(slot.AABB))
            {
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            slot.Position = position;
            System.Drawing.RectangleF mouseAABB = new System.Drawing.RectangleF(e.Position.X, e.Position.Y, 1, 1);
            if (mouseAABB.IntersectsWith(slot.AABB))
            {
                return true;
            }
            return false;
        }

        // Returns true if we're empty and can accept the atom passed in
        public bool CanAccept(Entity entity)
        {
            if (entity == null || entity.HasComponent(SS3D_shared.GO.ComponentFamily.Equippable))
                return false;
            //Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;
            //if ((tryAtom.IsChildOfType(atomType) || tryAtom.IsTypeOf(atomType)) &&
            //    (m.GetEquippedAtom(GetBodyPart()) == null))
            //    return true;
            //return false;
            return true;
        }

        public void Highlight()
        {
            highlight = true;
        }

        [Obsolete("TODO: Change to new system")]
        public override void Render()
        {
            if (highlight)
            {
                outline.Position = outlinePos;
                outline.Color = Color.Orange;
                outline.Draw();
                slot.Color = Color.Orange;
            }
            else
            {
                slot.Color = Color.White;
            }

            slot.Position = position;
            slot.Draw();

            if (highlight)
                text.Text.Color = Color.Orange;
            else
                text.Text.Color = Color.White;

            text.Position = new Point(position.X + 2, position.Y + 2);
            text.Text.ShadowColor = Color.Black;
            text.Text.Shadowed = true;
            text.Text.ShadowOffset = new Vector2D(1, 1);
            text.Update();
            
            Entity m = (Entity)playerController.controlledAtom;

            if (bodyPart != EquipmentSlot.None)
            {
                List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
                m.SendMessage(this, SS3D_shared.GO.ComponentMessageType.GetItemInEquipmentSlot, replies, bodyPart);
                if (replies.Count > 0 && replies[0].messageType == SS3D_shared.GO.ComponentMessageType.ReturnItemInEquipmentSlot)
                {
                    if ((Entity)replies[0].paramsList[0] != null)
                    {
                        Sprite s = Utilities.GetSpriteComponentSprite((Entity)replies[0].paramsList[0]);
                        s.Position = position;
                        s.Position += new Vector2D(slot.Width / 2f - s.Width / 2f, slot.Height / 2f - s.Height / 2f);
                        s.Draw();
                    }
                }
                
            }

            text.Render();

            // If we contain an atom then draw it in the appropriate place
            /*if (playerController.controlledAtom.IsChildOfType(typeof(Atom.Mob.Mob)))
            {
                if (bodyPart != GUIBodyPart.None)
                {
                    //Atom.Atom a = m.GetEquippedAtom(GetBodyPart());
                    //if (a != null)
                    //{
                         
                        a.sprite.Position = position;
                        a.sprite.Position += new Vector2D(slot.AABB.Width / 2, slot.AABB.Height / 2);
                        a.sprite.Draw();
                    //}
                }
            }*/ //TODO RE-ENABLE ITEM SLOTS WITH COMPONENTS
            highlight = false;
        }
    }
}
