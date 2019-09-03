using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.Interfaces.GameObjects.Components
{
    public interface ICollidableComponent : IComponent, IPhysBody
    {
        bool TryCollision(Vector2 offset, bool bump = false);
    }

    public interface ICollideSpecial
    {
        bool PreventCollide(IPhysBody collidedwith);
    }

    public interface ICollideBehavior
    {
        void CollideWith(List<IEntity> collidedwith);
    }
}
