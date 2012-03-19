using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using GorgonLibrary.Graphics;

namespace CGO
{
    public class PlayerAction
    {
        public readonly uint uid = 0;

        public PlayerActionTargetType targetType = PlayerActionTargetType.Any;

        public String name = "Empty Action";
        public String description = "This is an undefined Action.";
        public String icon = "action_none";

        public DateTime cooldownExpires;

        private PlayerActionComp parent;

        public PlayerAction(uint _uid, PlayerActionComp _parent) //Do not add more parameters to the constructors or bad things happen.
        {
            uid = _uid;
            parent = _parent;
        }

        public void Use(object target)
        {
            parent.SendDoAction(this, target);
        }
    }
}
