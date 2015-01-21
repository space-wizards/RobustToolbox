using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using GameObject;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Hitbox;

namespace SGO
{
    public class HitboxComponent : Component
    {
        public SizeF Size;
        public PointF Offset;

        public HitboxComponent()
        {
            Family = ComponentFamily.Hitbox;
            Size = new SizeF();
            Offset = new PointF();
        }

        public override ComponentState GetComponentState()
        {
            return new HitboxComponentState(Size, Offset);
        }

        /// <summary>
        /// Set parameters :)
        /// </summary>
        /// <param name="parameter"></param>
        public override void SetParameter(ComponentParameter parameter)
        {
            //base.SetParameter(parameter);
            switch (parameter.MemberName)
            {
                case "SizeX":
                    Size.Width = parameter.GetValue<float>();
                    break;
                case "SizeY":
                    Size.Height = parameter.GetValue<float>();
                    break;
                case "OffsetX":
                    Offset.X = parameter.GetValue<float>();
                    break;
                case "OffsetY":
                    Offset.Y = parameter.GetValue<float>();
                    break;
            }
        }
    }
}
