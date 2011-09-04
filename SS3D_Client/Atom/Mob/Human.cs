using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Mob
{
    public class Human : Mob
    {
        public Human()
            : base()
        {
            SetSpriteName(0, "Human");
            SetSpriteByIndex(0);
            /*meshName = "male_new.mesh";
            scale = new Mogre.Vector3(1f, 1f, 1f);
            offset = new Mogre.Vector3(0, 0, 0);*/
        }

        public override void MoveForward()
        {
            base.MoveForward();
        }

        public override void MoveBack()
        {
            base.MoveBack();
        }

        public override System.Drawing.RectangleF GetAABB()
        {
            return new System.Drawing.RectangleF(
                position.X - (sprite.AABB.Width / 2),
                position.Y,
                sprite.AABB.Width,
                (sprite.AABB.Height/2));
        }
    }
}
