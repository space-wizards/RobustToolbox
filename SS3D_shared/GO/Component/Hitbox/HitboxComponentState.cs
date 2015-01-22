using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SS13_Shared.GO.Component.Hitbox
{
    /// <summary>
    /// Hitbox is defined as the hight/width Size and pixel offset.
    /// The center of the hitbox is on the center of the entity it is attached to.
    /// The offset adjusts the position of the center of the hitbox.
    /// </summary>
    [Serializable]
    public class HitboxComponentState : ComponentState
    {
        public RectangleF AABB;

        public HitboxComponentState(RectangleF aabb)
            :base(ComponentFamily.Hitbox)
        {
            AABB = aabb;
        }
    }
}
