using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <inheritdoc />
    public class PhysicsManager : IPhysicsManager
    {
        private readonly List<IPhysBody> _results = new List<IPhysBody>();

        private readonly ConcurrentDictionary<MapId,BroadPhase> _treesPerMap =
            new ConcurrentDictionary<MapId, BroadPhase>();

        private BroadPhase this[MapId mapId] => _treesPerMap.GetOrAdd(mapId, _ => new BroadPhase());

        /// <summary>
        ///     returns true if collider intersects a physBody under management. Does not trigger Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <param name="map">Map ID to filter</param>
        /// <returns></returns>
        public bool IsColliding(Box2 collider, MapId map)
        {
            foreach (var body in this[map].Query(collider))
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
        internal bool DoCollisionTest(IPhysBody physBody, Box2 colliderAABB, List<IPhysBody> results)
        {
            // TODO: Terrible hack to fix bullets crashing the server.
            // Should be handled with deferred physics events instead.
            if(physBody.Owner.Deleted)
                return false;

            var any = false;

            foreach ( var body in this[physBody.MapID].Query(colliderAABB))
            {

                // TODO: Terrible hack to fix bullets crashing the server.
                // Should be handled with deferred physics events instead.
                if (body.Owner.Deleted) {
                    continue;
                }

                if (TestMask(physBody, body))
                {
                    results.Add(body);
                }

                any = true;
            }

            return any;
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

            return true;
        }

        /// <summary>
        ///     Adds a physBody to the manager.
        /// </summary>
        /// <param name="physBody"></param>
        public void AddBody(IPhysBody physBody)
        {
            if (!this[physBody.MapID].Add(physBody))
            {
                Logger.WarningS("phys", $"PhysicsBody already registered! {physBody.Owner}");
            }
        }

#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        /// <summary>
        ///     Removes a physBody from the manager
        /// </summary>
        /// <param name="physBody"></param>
        public void RemoveBody(IPhysBody physBody)
        {
            var removed = false;

            if (physBody.Owner.Deleted || physBody.Owner.Transform.Deleted)
            {
                foreach (var mapId in _mapManager.GetAllMapIds())
                {
                    removed = this[mapId].Remove(physBody);

                    if (removed)
                    {
                        break;
                    }
                }
            }

            if (!removed)
            {
                try
                {
                    removed = this[physBody.MapID].Remove(physBody);
                }
                catch (InvalidOperationException)
                {
                    // TODO: TryGetMapId or something
                    foreach (var mapId in _mapManager.GetAllMapIds())
                    {
                        removed = this[mapId].Remove(physBody);

                        if (removed)
                        {
                            break;
                        }
                    }
                }
            }

            if (!removed)
            {
                foreach (var mapId in _mapManager.GetAllMapIds())
                {
                    removed = this[mapId].Remove(physBody);

                    if (removed)
                    {
                        break;
                    }
                }
            }

            if (!removed)
                Logger.WarningS("phys", $"Trying to remove unregistered PhysicsBody! {physBody.Owner}");
        }

        /// <inheritdoc />
        public RayCastResults IntersectRay(MapId mapId, CollisionRay ray, float maxLength = 50, IEntity ignoredEnt = null)
        {
            RayCastResults result = default;

            this[mapId].Query((ref IPhysBody body, in Vector2 point, float distFromOrigin) => {

                if (distFromOrigin > maxLength)
                {
                    return true;
                }

                if (body.Owner == ignoredEnt)
                {
                    return true;
                }

                if (!body.CollisionEnabled)
                {
                    return true;
                }

                if ((body.CollisionLayer & ray.CollisionMask) == 0x0)
                {
                    return true;
                }

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (result.Distance != 0f && distFromOrigin > result.Distance)
                {
                    return true;
                }

                result = new RayCastResults(distFromOrigin, point, body.Owner);

                return true;
            }, ray.Position, ray.Direction);

            DebugDrawRay?.Invoke(new DebugRayData(ray, maxLength, result));

            return result;
        }

        public event Action<DebugRayData> DebugDrawRay;

        public IEnumerable<(IPhysBody, IPhysBody)> GetCollisions()
        {
            foreach (var mapId in _mapManager.GetAllMapIds())
            {
                foreach (var collision in this[mapId].GetCollisions(true))
                {
                    var (a, b) = collision;

                    if (!a.CollisionEnabled || !b.CollisionEnabled)
                    {
                        continue;
                    }

                    if (((a.CollisionLayer & b.CollisionMask) == 0x0)
                        ||(b.CollisionLayer & a.CollisionMask) == 0x0)
                    {
                        continue;
                    }

                    if (!a.WorldAABB.Intersects(b.WorldAABB))
                    {
                        continue;
                    }

                    yield return collision;
                }
            }
        }

        public bool Update(IPhysBody collider)
            => this[collider.MapID].Update(collider);

    }
}
