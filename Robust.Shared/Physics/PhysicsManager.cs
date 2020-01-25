using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <inheritdoc />
    public class PhysicsManager : IPhysicsManager
    {
        private readonly List<IPhysBody> _bodies = new List<IPhysBody>();
        private readonly List<IPhysBody> _results = new List<IPhysBody>();

        private readonly IBroadPhase _broadphase = new BroadPhaseNaive();

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

            // This will never collide with anything
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

            // collided with nothing
            if (_results.Count == 0)
                return false;

            //See if our collision will be overridden by a component
            var collisionmodifiers = entity.GetAllComponents<ICollideSpecial>().ToList();
            var collidedwith = new List<IEntity>();

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

            collidable.Bump(collidedwith);

            //TODO: This needs multi-grid support.
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
            // TODO: Terrible hack to fix bullets crashing the server.
            // Should be handled with deferred physics events instead.
            if(physBody.Owner.Deleted)
                return;

            _broadphase.Query((delegate(int id)
            {
                var body = _broadphase.GetProxy(id).Body;

                // TODO: Terrible hack to fix bullets crashing the server.
                // Should be handled with deferred physics events instead.
                if (body.Owner.Deleted)
                    return true;

                if (TestMask(physBody, body))
                    results.Add(body);

                return true;
            }), colliderAABB);
        }

        private static bool TestMask(IPhysBody a, IPhysBody b)
        {
            if (a == b)
                return false;

            if (!a.CollisionEnabled || !b.CollisionEnabled)
                return false;

            if ((a.CollisionMask & b.CollisionLayer) == 0x0 &&
                (b.CollisionMask & a.CollisionLayer) == 0x0)
                return false;

            return a.MapID == b.MapID;
        }

        /// <summary>
        ///     Adds a physBody to the manager.
        /// </summary>
        /// <param name="physBody"></param>
        public void AddBody(IPhysBody physBody)
        {
            if (!_bodies.Contains(physBody))
            {
                _bodies.Add(physBody);

                var proxy = new BodyProxy()
                {
                    Body = physBody
                };

                physBody.ProxyId = _broadphase.AddProxy(proxy);
            }
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
            {
                _bodies.Remove(physBody);
                _broadphase.RemoveProxy(physBody.ProxyId);
            }
            else
                Logger.WarningS("phys", $"Trying to remove unregistered PhysicsBody! {physBody.Owner}");
        }

        /// <inheritdoc />
        public RayCastResults IntersectRay(MapId mapId, Ray ray, float maxLength = 50, IEntity ignoredEnt = null)
        {
            RayCastResults rayResults = default;

            bool Callback(int proxy, RayCastResults results)
            {
                if (results.HitEntity == ignoredEnt)
                    return false;

                rayResults = results;
                return true;
            }

            _broadphase.RayCast(Callback, mapId, ray, maxLength);

            DebugDrawRay?.Invoke(new DebugRayData(ray, maxLength, rayResults));
            return rayResults;
        }

        public event Action<DebugRayData> DebugDrawRay;
    }
}
