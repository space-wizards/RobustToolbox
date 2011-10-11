using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CGO;

namespace SS3D.Atom.Mob
{
    public class Human : Mob
    {
        public Human()
            : base()
        {
            //SetSpriteName(0, "human_front");
            //SetSpriteByIndex(0);
            /*meshName = "male_new.mesh";
            scale = new Mogre.Vector3(1f, 1f, 1f);
            offset = new Mogre.Vector3(0, 0, 0);*/
        }

        public override void SetUp(int _uid, AtomManager _atomManager)
        {
            base.SetUp(_uid, _atomManager);

            if (equippedAtoms == null)
                equippedAtoms = new Dictionary<GUIBodyPart, Item.Item>();

            equippedAtoms.Add(GUIBodyPart.Inner, null);
            equippedAtoms.Add(GUIBodyPart.Feet, null);
            equippedAtoms.Add(GUIBodyPart.Hands, null);
            equippedAtoms.Add(GUIBodyPart.Belt, null);
            equippedAtoms.Add(GUIBodyPart.Eyes, null);
            equippedAtoms.Add(GUIBodyPart.Mask, null);
            equippedAtoms.Add(GUIBodyPart.Ears, null);
            equippedAtoms.Add(GUIBodyPart.Head, null);
            equippedAtoms.Add(GUIBodyPart.Outer, null);
            equippedAtoms.Add(GUIBodyPart.Back, null);
        }

        public override System.Drawing.RectangleF GetAABB()
        {
            return new System.Drawing.RectangleF(
                Position.X - (sprite.AABB.Width / 2),
                Position.Y,
                sprite.AABB.Width,
                (sprite.AABB.Height/2));
        }
    }
}
