using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects.Components
{
    public interface ICollidableComponent : IComponent, IPhysBody
    {

        bool IsColliding(Vector2 offset);

        IEnumerable<IEntity> GetCollidingEntities(Vector2 offset);
        bool UpdatePhysicsTree();

        void RemovedFromPhysicsTree(MapId mapId);
        void AddedToPhysicsTree(MapId mapId);
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
