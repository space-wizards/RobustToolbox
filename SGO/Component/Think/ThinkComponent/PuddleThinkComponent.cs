using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO;

namespace SGO.Component.Think.ThinkComponent
{
    public class PuddleThinkComponent : ThinkComponent
    {
        public override void OnBump(object sender, params object[] list)
        {
            base.OnBump(sender, list);
            ServerServices.LogManager.Log("Puddle Bumped!");
        }
    }
}
