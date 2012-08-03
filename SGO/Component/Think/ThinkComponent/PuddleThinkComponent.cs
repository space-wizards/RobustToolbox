using ServerServices.Log;
using ServerInterfaces.GameObject;
using SS13_Shared.GO;
using System.Collections.Generic;
using System;

namespace SGO.Component.Think.ThinkComponent
{
    public class PuddleThinkComponent : ThinkComponent
    {
        private Dictionary<IEntity, DateTime> recentlyAffected = new Dictionary<IEntity, DateTime>();

        public override void OnBump(object sender, params object[] list)
        {
            base.OnBump(sender, list);
            IEntity bumper = ((IEntity)list[0]);
            LogManager.Log("Puddle Bumped by " + bumper.Name);
            StatusEffectComp statComp = (StatusEffectComp)bumper.GetComponent(ComponentFamily.StatusEffects);

            if (statComp != null)
            {
                if(recentlyAffected.ContainsKey(bumper))
                {
                    if ((DateTime.Now - recentlyAffected[bumper]).Seconds > 5)
                    {
                        recentlyAffected[bumper] = DateTime.Now;
                        statComp.AddEffect("Rooted", 3);
                    }
                }
                else
                {
                    recentlyAffected.Add(bumper, DateTime.Now);
                    statComp.AddEffect("Rooted", 3);
                }
            }

        }

    }
}