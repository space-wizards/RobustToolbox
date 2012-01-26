using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
