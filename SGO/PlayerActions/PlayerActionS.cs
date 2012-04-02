using System;
using System.Drawing;
using SS13_Shared;
using ServerInterfaces.GameObject;

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

        public virtual void OnUse(IEntity targetEnt)
        {
            parent.StartCooldown(this);
        }
    }
}