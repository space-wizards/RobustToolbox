using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Maths;

namespace SS14.Shared.Physics
{
    class PhysicsBody
    {
        public ICollidable Collidable { get; set; }

        public PhysicsBody(ICollidable collidable)
        {
            Collidable = collidable;
        }
    }
}
