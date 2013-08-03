using System;
using System.Drawing;
using GameObject;
using SS13_Shared;

namespace SGO
{
    public class PlayerAction
    {
        public readonly uint uid;

        public DateTime cooldownExpires;
        public uint cooldownSeconds = 10;

        protected PlayerActionComp parent;
        public PlayerActionTargetType targetType = PlayerActionTargetType.Any;

        public PlayerAction(uint _uid, PlayerActionComp _parent)
        {
            parent = _parent;
            uid = _uid;
        }

        public virtual void OnUse(PointF targetPoint)
        {
            parent.StartCooldown(this);
        }

        public virtual void OnUse(Entity targetEnt)
        {
            parent.StartCooldown(this);
        }
    }
}