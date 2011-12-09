using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    public class TriggerableComponent : CollidableComponent
    {
        public TriggerableComponent()
            : base()
        {
            isHardCollidable = false;
        }
    }
}
