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
    public class ExampleAction : PlayerAction
    {
        public ExampleAction(uint _uid, PlayerActionComp _parent)
            : base(_uid, _parent)
        {
            name = "Stab";
            description = "Stab someone with one of the many hidden knifes you carry around.";
            icon = "action_stab";
        }
    }
}
