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
    public class ExampleEffect : StatusEffect
    {
        public ExampleEffect(uint _uid, Entity _affected) //Do not add more parameters to the constructors or bad things happen.
            : base(_uid, _affected)
        {
            name = "Example Effect";
            description = "This is an example...";
            icon = "status_example";
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
