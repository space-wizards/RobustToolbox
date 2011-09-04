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
        private GUIBodyPart bodyPart;
        private Type atomType;
        private Sprite slot;
        private TextSprite text;

        public ItemSlot(PlayerController _playerController, GUIBodyPart _bodyPart)
            : base(_playerController)
        {
            bodyPart = _bodyPart;
            slot = ResMgr.Singleton.GetSprite("GUISlot");
            text = new TextSprite("ItemSlot" + bodyPart, bodyPart.ToString(), ResMgr.Singleton.GetFont("CALIBRI"));
            position = new Point(0, 12);
            SetAtomType();
        }

        public void SetAtomType()
        {
            switch (bodyPart)
            {
                case GUIBodyPart.Ears:
                    atomType = typeof(Atom.Item.Tool.Crowbar);
                    break;
                case GUIBodyPart.Eyes:
                    atomType = typeof(Atom.Item.Tool.Welder);
                    break;
                case GUIBodyPart.None:
                    atomType = typeof(Atom.Item.Item);
                    break;
                case GUIBodyPart.Feet:
                case GUIBodyPart.Hands:
                case GUIBodyPart.Head:
                case GUIBodyPart.Inner:
                case GUIBodyPart.Mask:
                case GUIBodyPart.Outer:
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

        public bool CanAccept(Atom.Atom tryAtom)
        {
            Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;
            if ((tryAtom.IsChildOfType(atomType) || tryAtom.IsTypeOf(atomType)) &&
                (m.GetEquippedAtom(GetBodyPart()) == null))
                return true;
            return false;
        }

        public override void Render()
        {
            slot.Position = position;
            slot.Draw();

            text.Position = new Point(position.X + 2, position.Y + 2);
            text.Draw();


            Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;
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


        }
    }
}
