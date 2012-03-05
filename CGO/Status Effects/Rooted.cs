using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using GorgonLibrary.Graphics;
using ClientInterfaces;
using ClientInterfaces.Resource;
using SS13.IoC;

namespace CGO
{
    public class Rooted : StatusEffect
    {
        public Rooted(uint _uid, Entity _affected)
            : base(_uid, _affected)
        {
            name = "Rooted";
            description = "You can not move.";
            icon = "status_stun";
        }

        public override void OnAdd()
        {
        }

        public override void OnRemove()
        {
        }

        public override void OnUpdate()
        {
        }
    }
}
