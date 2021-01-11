using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private readonly ConcurrentDictionary<MapId,BroadPhase> _treesPerMap =
            new();

        private BroadPhase this[MapId mapId] => _treesPerMap.GetOrAdd(mapId, _ => new BroadPhase());

        /// <summary>
        ///     returns true if collider intersects a physBody under management.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <param name="map">Map ID to filter</param>
        /// <returns></returns>
        public bool TryCollideRect(Box2 collider, MapId map)
        {
            var state = (collider, map, found: false);
            this[map].QueryAabb(ref state, (ref (Box2 collider, MapId map, bool found) state, in IPhysBody body) =>
            {
                if (!body.CanCollide || body.CollisionLayer == 0x0)
                    return true;

                if (body.MapID == state.map &&
                    body.WorldAABB.Intersects(state.collider))
                {
                    state.found = true;
                    return false;
                }
                return true;
            }, collider, true);

            return state.found;
        }

        public bool IsWeightless(EntityCoordinates coordinates)
        {
            var gridId = coordinates.GetGridId(_entityManager);
            if (!gridId.IsValid())
            {
                // Not on a grid = no gravity for now.
                // In the future, may want to allow maps to override to always have gravity instead.
                return true;
            }

            var tile = _mapManager.GetGrid(gridId).GetTileRef(coordinates).Tile;
            return !_mapManager.GetGrid(gridId).HasGravity || tile.IsEmpty;
        }

        /// <summary>
        ///     Calculates the normal vector for two colliding bodies
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Vector2 CalculateNormal(IPhysBody target, IPhysBody source)
        {
            var manifold = target.WorldAABB.Intersect(source.WorldAABB);
            if (manifold.IsEmpty()) return Vector2.Zero;
            if (manifold.Height > manifold.Width)
            {
                // X is the axis of seperation
                var leftDist = source.WorldAABB.Right - target.WorldAABB.Left;
                var rightDist = target.WorldAABB.Right - source.WorldAABB.Left;
                return new Vector2(leftDist > rightDist ? 1 : -1, 0);
            }
            else
            {
                // Y is the axis of seperation
                var bottomDist = source.WorldAABB.Top - target.WorldAABB.Bottom;
                var topDist = target.WorldAABB.Top - source.WorldAABB.Bottom;
                return new Vector2(0, bottomDist > topDist ? 1 : -1);
            }
        }

        public float CalculatePenetration(IPhysBody target, IPhysBody source)
        {
            var manifold = target.WorldAABB.Intersect(source.WorldAABB);
            if (manifold.IsEmpty()) return 0.0f;
            return manifold.Height > manifold.Width ? manifold.Width : manifold.Height;
        }

        // Impulse resolution algorithm based on Box2D's approach in combination with Randy Gaul's Impulse Engine resolution algorithm.
        public Vector2 SolveCollisionImpulse(Manifold manifold)
        {
            var aP = manifold.A;
            var bP = manifold.B;

            if (!aP.CanMove() && !bP.CanMove()) return Vector2.Zero;

            var restitution = 0.01f;
            var normal = manifold.Normal;
            var rV = manifold.RelativeVelocity;

            var vAlongNormal = Vector2.Dot(rV, normal);
            if (vAlongNormal > 0)
            {
                return Vector2.Zero;
            }

            var impulse = -(1.0f + restitution) * vAlongNormal;
            impulse /= aP.InvMass + bP.InvMass;

            return manifold.Normal * impulse;
        }

        public IEnumerable<IEntity> GetCollidingEntities(IPhysBody physBody, Vector2 offset, bool approximate = true)
        {
            var modifiers = physBody.Entity.GetAllComponents<ICollideSpecial>();
            var entities = new List<IEntity>();

            var state = (physBody, modifiers, entities);

            this[physBody.MapID].QueryAabb(ref state,
                (ref (IPhysBody physBody, IEnumerable<ICollideSpecial> modifiers, List<IEntity> entities) state,
                    in IPhysBody body) =>
            {
                if (body.Entity.Deleted) {
                    return true;
                }

                if (CollidesOnMask(state.physBody, body))
                {
                    var preventCollision = false;
                    var otherModifiers = body.Entity.GetAllComponents<ICollideSpecial>();
                    foreach (var modifier in state.modifiers)
                    {
                        preventCollision |= modifier.PreventCollide(body);
                    }
                    foreach (var modifier in otherModifiers)
                    {
                        preventCollision |= modifier.PreventCollide(state.physBody);
                    }

                    if (preventCollision)
                    {
                        return true;
                    }
                    state.entities.Add(body.Entity);
                }
                return true;
            }, physBody.WorldAABB, approximate);

            return entities;
        }

        /// <inheritdoc />
        public IEnumerable<IPhysBody> GetCollidingEntities(MapId mapId, in Box2 worldBox)
        {
            return this[mapId].QueryAabb(worldBox, false);
        }

        public bool IsColliding(IPhysBody body, Vector2 offset, bool approximate)
        {
            return GetCollidingEntities(body, offset, approximate).Any();
        }

        public static bool CollidesOnMask(IPhysBody a, IPhysBody b)
        {
            if (a == b)
                return false;

            if (!a.CanCollide || !b.CanCollide)
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
                Logger.WarningS("phys", $"PhysicsBody already registered! {physBody.Entity}");
            }
        }

        /// <summary>
        ///     Removes a physBody from the manager
        /// </summary>
        /// <param name="physBody"></param>
        public void RemoveBody(IPhysBody physBody)
        {
            var removed = false;

            if (physBody.Entity.Deleted || physBody.Entity.Transform.Deleted)
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
                Logger.WarningS("phys", $"Trying to remove unregistered PhysicsBody! {physBody.Entity.Uid}");
        }

        /// <inheritdoc />
        public IEnumerable<RayCastResults> IntersectRayWithPredicate(MapId mapId, CollisionRay ray,
            float maxLength = 50F,
            Func<IEntity, bool>? predicate = null, bool returnOnFirstHit = true)
        {
            List<RayCastResults> results = new();

            this[mapId].QueryRay((in IPhysBody body, in Vector2 point, float distFromOrigin) =>
            {

                if (returnOnFirstHit && results.Count > 0) return true;

                if (distFromOrigin > maxLength)
                {
                    return true;
                }

                if (!body.CanCollide)
                {
                    return true;
                }

                if ((body.CollisionLayer & ray.CollisionMask) == 0x0)
                {
                    return true;
                }

                if (predicate != null && predicate.Invoke(body.Entity))
                {
                    return true;
                }

                var result = new RayCastResults(distFromOrigin, point, body.Entity);
                results.Add(result);
                DebugDrawRay?.Invoke(new DebugRayData(ray, maxLength, result));
                return true;
            }, ray);
            if (results.Count == 0)
            {
                DebugDrawRay?.Invoke(new DebugRayData(ray, maxLength, null));
            }

            results.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return results;
        }

        /// <inheritdoc />
        public IEnumerable<RayCastResults> IntersectRay(MapId mapId, CollisionRay ray, float maxLength = 50, IEntity? ignoredEnt = null, bool returnOnFirstHit = true)
            => IntersectRayWithPredicate(mapId, ray, maxLength, entity => entity == ignoredEnt, returnOnFirstHit);

        /// <inheritdoc />
        public float IntersectRayPenetration(MapId mapId, CollisionRay ray, float maxLength, IEntity? ignoredEnt = null)
        {
            var penetration = 0f;

            this[mapId].QueryRay((in IPhysBody body, in Vector2 point, float distFromOrigin) =>
            {
                if (distFromOrigin > maxLength)
                {
                    return true;
                }

                if (!body.CanCollide)
                {
                    return true;
                }

                if ((body.CollisionLayer & ray.CollisionMask) == 0x0)
                {
                    return true;
                }

                if (new Ray(point + ray.Direction * body.WorldAABB.Size.Length * 2, -ray.Direction).Intersects(
                    body.WorldAABB, out _, out var exitPoint))
                {
                    penetration += (point - exitPoint).Length;
                }
                return true;
            }, ray);

            return penetration;
        }

        public event Action<DebugRayData>? DebugDrawRay;

        public bool Update(IPhysBody collider)
        {
            collider.WakeBody();
            return this[collider.MapID].Update(collider);
        }

        public void RemovedFromMap(IPhysBody body, MapId mapId)
        {
            body.WakeBody();
            this[mapId].Remove(body);
        }

        public void AddedToMap(IPhysBody body, MapId mapId)
        {
            body.WakeBody();
            this[mapId].Add(body);
        }

        /// <summary>
        /// How many ticks before a physics body will go to sleep. Bodies will only sleep if
        /// they have no velocity.
        /// </summary>
        /// <remarks>
        /// This is an arbitrary number greater than zero. To solve "locker stacks" that span multiple ticks,
        /// this needs to be greater than one. Every time an entity collides or is moved, the body's <see cref="IPhysBody.SleepAccumulator"/>
        /// goes back to zero.
        /// </remarks>
        public int SleepTimeThreshold { get; set; } = 2;
    }
}
