using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Organs = SGO.Component.Health.Organs;

namespace SGO
{
    public class HealthComponent : GameObjectComponent
    {
        public Organs.BLOOD_TYPE blood_type = Organs.BLOOD_TYPE.A; // Temporary
        public List<Organs.Organ> organs = new List<Organs.Organ>();

        public override void  Update(float frameTime)
        {
 	        base.Update(frameTime);
        
            foreach (Organs.Organ organ in organs)
            {
                organ.Process(frameTime);
            }
        }
    }
}
