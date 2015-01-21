using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using GameObject;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Hitbox;

namespace CGO
{
    public class HitboxComponent : Component
    {
        public SizeF Size;
        public PointF Offset;
        public PointF UpperLeft
        {
            get
            {
                return new PointF(Offset.X - Size.Width / 2, Offset.Y - Size.Height / 2);
            }
        }
        public RectangleF AABB
        {
            get
            {
                return new RectangleF(UpperLeft, Size);
            }
        }

        public HitboxComponent()
        {
            Family = ComponentFamily.Hitbox;
            Size = new SizeF();
            Offset = new PointF();
        }

        public override Type StateType
        {
            get
            {
                return typeof(HitboxComponentState);
            }
        }

        public override void HandleComponentState(dynamic state)
        {
            Size = state.Size;
            Offset = state.Offset;
        }
    }
}
