using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using SS14.Shared.Interfaces.Physics;

namespace SS14.Shared.Interfaces.GameObjects.Components
{
    public interface ICollidableComponent : IComponent, ICollidable
    {
        bool TryCollision(Vector2 offset, bool bump = false);
    }
}
