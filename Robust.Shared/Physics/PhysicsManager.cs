using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network.Messages;

namespace Robust.Shared.Physics
{
    /// <inheritdoc />
    public class PhysicsManager : IPhysicsManager
    {
        private readonly List<IPhysBody> _bodies = new List<IPhysBody>();
        private readonly List<IPhysBody> _results = new List<IPhysBody>();

        /// <summary>
        ///     returns true if collider intersects a physBody under management. Does not trigger Bump.
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
        ///     returns true if collider intersects a physBody under management and calls Bump.
        /// </summary>
        /// <param name="entity">Rectangle to check for collision</param>
        /// <param name="offset"></param>
        /// <param name="bump"></param>
        /// <returns></returns>
        public bool TryCollide(IEntity entity, Vector2 offset, bool bump = true)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var collidable = (IPhysBody) entity.GetComponent<ICollidableComponent>();

            if (!collidable.CollisionEnabled || collidable.CollisionLayer == 0x0)
                return false;

            var colliderAABB = collidable.WorldAABB;
            if (offset.LengthSquared > 0)
            {
                colliderAABB = colliderAABB.Translated(offset);
            }

            // Test this physBody against every other one.
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
                if (!otherCollidable.IsHardCollidable)
                {
                    continue;
                }

                //Provides component level overrides for collision behavior based on the entity we are trying to collide with
                var preventcollision = false;
                foreach (var mods in collisionmodifiers)
                {
                    preventcollision |= mods.PreventCollide(otherCollidable);
                }
                if (preventcollision)
                {
                    continue;
                }

                collided = true;

                if (!bump)
                {
                    continue;
                }

                otherCollidable.Bumped(entity);
                collidedwith.Add(otherCollidable.Owner);
            }

            return collided;
        }

        /// <summary>
        ///     Tests a physBody against every other registered physBody.
        /// </summary>
        /// <param name="physBody">Body being tested.</param>
        /// <param name="colliderAABB">The AABB of the physBody being tested. This can be IPhysBody.WorldAABB, or a modified version of it.</param>
        /// <param name="results">An empty list that the function stores all colliding bodies inside of.</param>
        internal void DoCollisionTest(IPhysBody physBody, Box2 colliderAABB, List<IPhysBody> results)
        {
            foreach (var body in GetCollidablesForLocation(colliderAABB))
            {
                // TODO: Terrible hack to fix bullets crashing the server.
                // Should be handled with deferred physics events instead.
                if (body.Owner.Deleted)
                {
                    continue;
                }

                if (!body.CollisionEnabled)
                {
                    continue;
                }

                if ((physBody.CollisionMask & body.CollisionLayer) == 0x0)
                {
                    continue;
                }

                if (physBody.MapID != body.MapID ||
                    physBody == body ||
                    !colliderAABB.Intersects(body.WorldAABB))
                {
                    continue;
                }

                results.Add(body);
            }
        }

        /// <summary>
        ///     Adds a physBody to the manager.
        /// </summary>
        /// <param name="physBody"></param>
        public void AddBody(IPhysBody physBody)
        {
            if (!_bodies.Contains(physBody))
                _bodies.Add(physBody);
            else
                Logger.WarningS("phys", $"PhysicsBody already registered! {physBody.Owner}");
        }

        /// <summary>
        ///     Removes a physBody from the manager
        /// </summary>
        /// <param name="physBody"></param>
        public void RemoveBody(IPhysBody physBody)
        {
            if (_bodies.Contains(physBody))
                _bodies.Remove(physBody);
            else
                Logger.WarningS("phys", $"Trying to remove unregistered PhysicsBody! {physBody.Owner}");
        }

        /// <inheritdoc />
        public RayCastResults IntersectRay(Ray ray, float maxLength = 50, IEntity ignoredEnt = null)
        {
            IEntity entity = null;
            var hitPosition = Vector2.Zero;
            var minDist = maxLength;
            foreach (var body in _bodies)
            {
                if ((ray.CollisionMask & body.CollisionLayer) == 0x0)
                {
                    continue;
                }
                if (ray.Intersects(body.WorldAABB, out var dist, out var hitPos) && dist < minDist)
                {
                    if (!body.IsHardCollidable || ignoredEnt != null && ignoredEnt == body.Owner)
                        continue;

                    entity = body.Owner;
                    minDist = dist;
                    hitPosition = hitPos;
                }
            }

            RayCastResults results = default;
            if (entity != null)
            {
                results = new RayCastResults(minDist, hitPosition, entity);
            }

            DebugDrawRay?.Invoke(new DebugRayData(ray, maxLength, results));

            return results;
        }

        public event Action<DebugRayData> DebugDrawRay;

        private Dictionary<Vector2, List<IPhysBody>> _collisionGrid = new Dictionary<Vector2, List<IPhysBody>>();
        private float _collisionGridResolution = 5;

        public void BuildCollisionGrid()
        {
            foreach (var bodies in _collisionGrid.Values)
            {
                bodies.Clear();
            }

            foreach (var body in _bodies)
            {
                var snappedLocation = SnapLocationToGrid(body.WorldAABB);
                if (!_collisionGrid.TryGetValue(snappedLocation, out var bodies))
                {
                    bodies = new List<IPhysBody>();
                    _collisionGrid.Add(snappedLocation, bodies);
                }

                bodies.Add(body);
            }
        }

        private Vector2 SnapLocationToGrid(Box2 worldAAAB)
        {
            var result = worldAAAB.Center;
            result /= _collisionGridResolution;
            result = new Vector2((float)Math.Floor(result.X), (float)Math.Floor(result.Y));
            return result;
        }

        private IEnumerable<IPhysBody> GetCollidablesForLocation(Box2 location)
        {
            var snappedLocation = SnapLocationToGrid(location);

            for (var xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (var yOffset = -1; yOffset <= 1; yOffset++)
                {
                    var offsetLocation = snappedLocation + new Vector2(xOffset, yOffset);
                    if (!_collisionGrid.TryGetValue(offsetLocation, out var bodies))
                    {
                        continue;
                    }

                    foreach (var body in bodies)
                    {
                        yield return body;
                    }
                }
            }
        }
    }
}
