using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Shared.Physics
{
    /// <inheritdoc />
    public class PhysicsManager : IPhysicsManager
    {
        private readonly List<ICollidable> _bodies = new List<ICollidable>();
        private readonly List<ICollidable> _results = new List<ICollidable>();

        /// <summary>
        ///     returns true if collider intersects a collidable under management. Does not trigger Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <param name="map">Map ID to filter</param>
        /// <returns></returns>
        public bool IsColliding(Box2 collider, MapId map)
        {
            foreach (var body in _bodies)
            {
                if (!body.CollisionEnabled || body.CollisionLayer == 0x0)
                    continue;

                if (body.MapID == map &&
                    body.IsHardCollidable &&
                    body.WorldAABB.Intersects(collider))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     returns true if collider intersects a collidable under management and calls Bump.
        /// </summary>
        /// <param name="entity">Rectangle to check for collision</param>
        /// <param name="offset"></param>
        /// <param name="bump"></param>
        /// <returns></returns>
        public bool TryCollide(IEntity entity, Vector2 offset, bool bump = true)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var collidable = (ICollidable) entity.GetComponent<ICollidableComponent>();

            if (!collidable.CollisionEnabled || collidable.CollisionLayer == 0x0)
                return false;

            var colliderAABB = collidable.WorldAABB;
            if (offset.LengthSquared > 0)
            {
                colliderAABB = colliderAABB.Translated(offset);
            }

            // Test this collidable against every other one.
            _results.Clear();
            DoCollisionTest(collidable, colliderAABB, _results);

            //See if our collision will be overridden by a component
            var collisionmodifiers = entity.GetAllComponents<ICollideSpecial>().ToList();
            var collidedwith = new List<IEntity>();

            var collided = TestSpecialCollisionAndBump(entity, bump, collisionmodifiers, collidedwith);

            collidable.Bump(collidedwith);

            //TODO: This needs multi-grid support.
            return collided;
        }

        private bool TestSpecialCollisionAndBump(IEntity entity, bool bump, List<ICollideSpecial> collisionmodifiers, List<IEntity> collidedwith)
        {
            //try all of the AABBs against the target rect.
            var collided = false;
            foreach (var otherCollidable in _results)
            {
                //Provides component level overrides for collision behavior based on the entity we are trying to collide with
                var preventcollision = false;

                foreach (var mods in collisionmodifiers)
                    preventcollision |= mods.PreventCollide(otherCollidable);

                if (preventcollision) //We were prevented, bail
                    continue;

                if (!otherCollidable.IsHardCollidable)
                    continue;

                collided = true;

                if (!bump)
                    continue;

                otherCollidable.Bumped(entity);
                collidedwith.Add(otherCollidable.Owner);
            }

            return collided;
        }

        /// <summary>
        ///     Tests a collidable against every other registered collidable.
        /// </summary>
        /// <param name="collidable">Collidable being tested.</param>
        /// <param name="colliderAABB">The AABB of the collidable being tested. This can be ICollidable.WorldAABB, or a modified version of it.</param>
        /// <param name="results">An empty list that the function stores all colliding bodies inside of.</param>
        public void DoCollisionTest(ICollidable collidable, Box2 colliderAABB, List<ICollidable> results)
        {
            foreach (var body in _bodies)
            {
                if(!body.CollisionEnabled)
                    continue;

                if ((collidable.CollisionMask & body.CollisionLayer) == 0x0)
                    continue;

                if (collidable.MapID != body.MapID ||
                    collidable == body ||
                    !colliderAABB.Intersects(body.WorldAABB))
                    continue;

                results.Add(body);
            }
        }

        /// <summary>
        ///     Adds a collidable to the manager.
        /// </summary>
        /// <param name="collidable"></param>
        public void AddCollidable(ICollidable collidable)
        {
            if (!_bodies.Contains(collidable))
                _bodies.Add(collidable);
            else
                Logger.WarningS("phys", $"Collidable already registered! {collidable.Owner}");
        }

        /// <summary>
        ///     Removes a collidable from the manager
        /// </summary>
        /// <param name="collidable"></param>
        public void RemoveCollidable(ICollidable collidable)
        {
            if (_bodies.Contains(collidable))
                _bodies.Remove(collidable);
            else
                Logger.WarningS("phys", $"Trying to remove unregistered collidable! {collidable.Owner}");
        }

        /// <inheritdoc />
        public RayCastResults IntersectRay(Ray ray, float maxLength = 50, IEntity ignoredEnt = null)
        {
            IEntity entity = null;
            var hitPosition = Vector2.Zero;
            var minDist = maxLength;

            foreach (var body in _bodies)
            {
                if (ray.Intersects(body.WorldAABB, out var dist, out var hitPos) && dist < minDist)
                {
                    if (ignoredEnt != null && ignoredEnt == body.Owner)
                        continue;

                    entity = body.Owner;
                    minDist = dist;
                    hitPosition = hitPos;
                }
            }

            if (entity != null)
                return new RayCastResults(minDist, hitPosition, entity);

            return default;
        }
    }
}
