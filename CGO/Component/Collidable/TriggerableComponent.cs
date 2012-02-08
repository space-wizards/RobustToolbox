using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CGO.Component.Collidable;

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
