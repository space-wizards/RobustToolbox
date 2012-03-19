using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Reflection;
using System.Collections;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using System.Drawing;

namespace SGO
{
    public class PlayerAction
    {
        public readonly uint uid = 0;

        public PlayerActionTargetType targetType = PlayerActionTargetType.Any;

        public uint cooldownSeconds = 10;
        public DateTime cooldownExpires;

        protected PlayerActionComp parent;

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
