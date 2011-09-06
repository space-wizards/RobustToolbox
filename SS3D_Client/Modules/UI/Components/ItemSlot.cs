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

namespace SS3D.Modules.UI.Components
{
    public class ItemSlot : GuiComponent
    {
        private GUIBodyPart bodyPart; // The bodypart we reference
        private Type atomType; // The type of atoms we can accept
        private Sprite slot;
        private TextSprite text;
        private bool highlight = false;

        public ItemSlot(PlayerController _playerController, GUIBodyPart _bodyPart)
            : base(_playerController)
        {
            bodyPart = _bodyPart;
            slot = UIDesktop.Singleton.Skin.Elements["Window.InventorySlot"].GetSprite();
            text = new TextSprite("ItemSlot" + bodyPart, bodyPart.ToString(), ResMgr.Singleton.GetFont("CALIBRI"));
            position = new Point(0, 12);
            SetAtomType();
        }

        public void SetAtomType()
        {
            switch (bodyPart)
            {
                case GUIBodyPart.Feet:
                    atomType = typeof(Atom.Item.Wearable.Feet.Feet);
                    break;
                case GUIBodyPart.Inner:
                    atomType = typeof(Atom.Item.Wearable.Inner.Inner);
                    break;
                case GUIBodyPart.Ears:
                    atomType = typeof(Atom.Item.Wearable.Ears.Ears);
                    break;
                case GUIBodyPart.Eyes:
                    atomType = typeof(Atom.Item.Wearable.Eyes.Eyes);
                    break;
                case GUIBodyPart.Hands:
                    atomType = typeof(Atom.Item.Wearable.Hands.Hands);
                    break;
                case GUIBodyPart.Head:
                    atomType = typeof(Atom.Item.Wearable.Head.Head);
                    break;
                case GUIBodyPart.Mask:
                    atomType = typeof(Atom.Item.Wearable.Mask.Mask);
                    break;
                case GUIBodyPart.Outer:
                    atomType = typeof(Atom.Item.Wearable.Outer.Outer);
                    break;
                case GUIBodyPart.Belt:
                    atomType = typeof(Atom.Item.Wearable.Belt.Belt);
                    break;
                case GUIBodyPart.Back:
                case GUIBodyPart.None:
                default:
                    atomType = typeof(Atom.Item.Tool.Wrench);
                    break;
            }
        }

        public GUIBodyPart GetBodyPart()
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
        public bool CanAccept(Atom.Atom tryAtom)
        {
            if (tryAtom == null)
                return false;
            Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;
            if ((tryAtom.IsChildOfType(atomType) || tryAtom.IsTypeOf(atomType)) &&
                (m.GetEquippedAtom(GetBodyPart()) == null))
                return true;
            return false;
        }

        public void Highlight()
        {
            highlight = true;
        }

        public override void Render()
        {
            if (highlight)
                slot.Color = Color.Orange;
            slot.Position = position;
            slot.Draw();
            if(highlight)
                slot.Color = Color.White;

            text.Position = new Point(position.X + 2, position.Y + 2);
            text.Draw();
            

            Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;
            // If we contain an atom then draw it in the appropriate place
            if (playerController.controlledAtom.IsChildOfType(typeof(Atom.Mob.Mob)))
            {
                if (bodyPart != GUIBodyPart.None)
                {
                    Atom.Atom a = m.GetEquippedAtom(GetBodyPart());
                    if (a != null)
                    {
                        a.sprite.Position = position;
                        a.sprite.Position += new Vector2D(slot.AABB.Width / 2, slot.AABB.Height / 2);
                        a.sprite.Draw();
                    }
                }
            }
            highlight = false;

        }
    }
}
