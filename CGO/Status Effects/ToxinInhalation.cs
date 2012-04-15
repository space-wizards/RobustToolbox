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
    public class ToxinInhalation : StatusEffect
    {
        public ToxinInhalation(uint _uid, Entity _affected)
            : base(_uid, _affected)
        {
            name = "Toxin Inhalation";
            description = "You have inhaled toxins." + Environment.NewLine + "Causes additional damage over time.";
            icon = "status_toxinh";
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
