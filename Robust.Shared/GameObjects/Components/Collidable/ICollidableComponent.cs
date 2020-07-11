using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects.Components
{
    public interface ICollidableComponent : IComponent, IPhysBody
    {
        public bool Hard { get; set; }
        bool IsColliding(Vector2 offset, bool approximate = true);

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
        void CollideWith(IEntity collidedWith);

        /// <summary>
        ///     Called after all collisions have been processed, as well as how many collisions occured
        /// </summary>
        /// <param name="collisionCount"></param>
        void PostCollide(int collisionCount) { }
    }
}
