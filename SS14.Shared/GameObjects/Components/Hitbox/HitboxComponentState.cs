using System;
using SFML.Graphics;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    ///     Hitbox is defined as the hight/width Size and pixel offset.
    ///     The center of the hitbox is on the center of the entity it is attached to.
    ///     The offset adjusts the position of the center of the hitbox.
    /// </summary>
    [Serializable]
    public class HitboxComponentState : ComponentState
    {
        public readonly FloatRect AABB;

        public HitboxComponentState(FloatRect aabb)
            : base(NetIDs.HITBOX)
        {
            AABB = aabb;
        }
    }
}
