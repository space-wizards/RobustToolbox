using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.GameObjects
{
    /*
     * Handles all of the public query methods for physics.
     */
    public partial class SharedPhysicsSystem
    {
        [Dependency] private readonly SharedBroadphaseSystem _broadphaseSystem = default!;

        /// <summary>
        ///     Get the percentage that 2 bodies overlap. Ignores whether collision is turned on for either body.
        /// </summary>
        /// <param name="bodyA"></param>
        /// <param name="bodyB"></param>
        /// <returns> 0 -> 1.0f based on WorldAABB overlap</returns>
        public float IntersectionPercent(PhysicsComponent bodyA, PhysicsComponent bodyB)
        {
            // TODO: Use actual shapes and not just the AABB?
            return bodyA.GetWorldAABB().IntersectPercentage(bodyB.GetWorldAABB());
        }

        /// <summary>
        /// Checks to see if the specified collision rectangle collides with any of the physBodies under management.
        /// Also fires the OnCollide event of the first managed physBody to intersect with the collider.
        /// </summary>
        /// <param name="collider">Collision rectangle to check</param>
        /// <param name="mapId">Map to check on</param>
        /// <param name="approximate"></param>
        /// <returns>true if collides, false if not</returns>
        public bool TryCollideRect(Box2 collider, MapId mapId, bool approximate = true)
        {
            var state = (collider, mapId, found: false);

            foreach (var broadphase in _broadphaseSystem.GetBroadphases(mapId, collider))
            {
                var gridCollider = broadphase.Owner.Transform.InvWorldMatrix.TransformBox(collider);

                broadphase.Tree.QueryAabb(ref state, (ref (Box2 collider, MapId map, bool found) state, in FixtureProxy proxy) =>
                {
                    if (proxy.Fixture.CollisionLayer == 0x0)
                        return true;

                    if (proxy.AABB.Intersects(gridCollider))
                    {
                        state.found = true;
                        return false;
                    }
                    return true;
                }, gridCollider, approximate);
            }

            return state.found;
        }

        public IEnumerable<PhysicsComponent> GetCollidingEntities(PhysicsComponent body, Vector2 offset, bool approximate = true)
        {
            var broadphase = body.Broadphase;
            if (broadphase == null)
            {
                return Array.Empty<PhysicsComponent>();
            }

            var entities = new List<PhysicsComponent>();

            var state = (body, entities);

            foreach (var fixture in body.Fixtures)
            {
                foreach (var proxy in fixture.Proxies)
                {
                    broadphase.Tree.QueryAabb(ref state,
                        (ref (PhysicsComponent body, List<PhysicsComponent> entities) state,
                            in FixtureProxy other) =>
                        {
                            if (other.Fixture.Body.Deleted || other.Fixture.Body == body) return true;
                            if ((proxy.Fixture.CollisionMask & other.Fixture.CollisionLayer) == 0x0) return true;
                            if (!body.ShouldCollide(other.Fixture.Body)) return true;

                            state.entities.Add(other.Fixture.Body);
                            return true;
                        }, proxy.AABB, approximate);
                }
            }

            return entities;
        }

        /// <summary>
        /// Get all entities colliding with a certain body.
        /// </summary>
        public IEnumerable<PhysicsComponent> GetCollidingEntities(MapId mapId, in Box2 worldAABB)
        {
            if (mapId == MapId.Nullspace) return Array.Empty<PhysicsComponent>();

            var bodies = new HashSet<PhysicsComponent>();

            foreach (var broadphase in _broadphaseSystem.GetBroadphases(mapId, worldAABB))
            {
                var gridAABB = broadphase.Owner.Transform.InvWorldMatrix.TransformBox(worldAABB);

                foreach (var proxy in broadphase.Tree.QueryAabb(gridAABB, false))
                {
                    bodies.Add(proxy.Fixture.Body);
                }
            }

            return bodies;
        }

        /// <summary>
        /// Get all entities colliding with a certain body.
        /// </summary>
        public IEnumerable<PhysicsComponent> GetCollidingEntities(MapId mapId, in Box2Rotated worldBounds)
        {
            if (mapId == MapId.Nullspace) return Array.Empty<PhysicsComponent>();

            var bodies = new HashSet<PhysicsComponent>();

            foreach (var broadphase in _broadphaseSystem.GetBroadphases(mapId, worldBounds.CalcBoundingBox()))
            {
                var gridAABB = broadphase.Owner.Transform.InvWorldMatrix.TransformBox(worldBounds);

                foreach (var proxy in broadphase.Tree.QueryAabb(gridAABB, false))
                {
                    bodies.Add(proxy.Fixture.Body);
                }
            }

            return bodies;
        }

        // TODO: This is mainly just a hack to get rotated grid doors working in SS14 but whenever we do querysystem we'll clean this shit up
        /// <summary>
        /// Returns all enabled physics bodies intersecting this body.
        /// </summary>
        /// <remarks>
        /// Does not consider CanCollide on the provided body.
        /// </remarks>
        /// <param name="body">The body to check for intersection</param>
        /// <param name="enlarge">How much to enlarge / shrink the bounds by given we often extend them slightly.</param>
        /// <returns></returns>
        public IEnumerable<PhysicsComponent> GetCollidingEntities(PhysicsComponent body, float enlarge = 0f)
        {
            // TODO: Should use the collisionmanager test for overlap instead (once I optimise and check it actually works).
            var mapId = body.Owner.Transform.MapID;

            if (mapId == MapId.Nullspace || body.FixtureCount == 0) return Array.Empty<PhysicsComponent>();

            var bodies = new HashSet<PhysicsComponent>();
            var transform = body.GetTransform();
            var worldAABB = body.GetWorldAABB(transform.Position, transform.Quaternion2D.Angle);

            foreach (var broadphase in _broadphaseSystem.GetBroadphases(mapId, worldAABB))
            {
                var invMatrix = broadphase.Owner.Transform.InvWorldMatrix;

                var localTransform = new Transform(invMatrix.Transform(transform.Position), transform.Quaternion2D.Angle - broadphase.Owner.Transform.WorldRotation);

                foreach (var fixture in body.Fixtures)
                {
                    var collisionMask = fixture.CollisionMask;
                    var collisionLayer = fixture.CollisionLayer;

                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        var aabb = fixture.Shape.ComputeAABB(localTransform, i).Enlarged(enlarge);

                        foreach (var proxy in broadphase.Tree.QueryAabb(aabb, false))
                        {
                            var proxyFixture = proxy.Fixture;
                            var proxyBody = proxyFixture.Body;

                            if (proxyBody == body ||
                                (proxyFixture.CollisionMask & collisionLayer) == 0x0 &&
                                (collisionMask & proxyFixture.CollisionLayer) == 0x0) continue;

                            bodies.Add(proxyBody);
                        }
                    }
                }
            }

            return bodies;
        }

        // TODO: This will get every body but we don't need to do that
        /// <summary>
        ///     Checks whether a body is colliding
        /// </summary>
        /// <param name="body"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public bool IsColliding(PhysicsComponent body, Vector2 offset, bool approximate)
        {
            return GetCollidingEntities(body, offset, approximate).Any();
        }

        #region RayCast
        /// <summary>
        ///     Casts a ray in the world, returning the first entity it hits (or all entities it hits, if so specified)
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <param name="predicate">A predicate to check whether to ignore an entity or not. If it returns true, it will be ignored.</param>
        /// <param name="returnOnFirstHit">If true, will only include the first hit entity in results. Otherwise, returns all of them.</param>
        /// <returns>A result object describing the hit, if any.</returns>
        public IEnumerable<RayCastResults> IntersectRayWithPredicate(MapId mapId, CollisionRay ray,
            float maxLength = 50F,
            Func<IEntity, bool>? predicate = null, bool returnOnFirstHit = true)
        {
            List<RayCastResults> results = new();
            var endPoint = ray.Position + ray.Direction.Normalized * maxLength;
            var rayBox = new Box2(Vector2.ComponentMin(ray.Position, endPoint),
                Vector2.ComponentMax(ray.Position, endPoint));

            foreach (var broadphase in _broadphaseSystem.GetBroadphases(mapId, rayBox))
            {
                var invMatrix = broadphase.Owner.Transform.InvWorldMatrix;
                var matrix = broadphase.Owner.Transform.WorldMatrix;

                var position = invMatrix.Transform(ray.Position);
                var gridRot = new Angle(-broadphase.Owner.Transform.WorldRotation.Theta);
                var direction = gridRot.RotateVec(ray.Direction);

                var gridRay = new CollisionRay(position, direction, ray.CollisionMask);

                broadphase.Tree.QueryRay((in FixtureProxy proxy, in Vector2 point, float distFromOrigin) =>
                {
                    if (returnOnFirstHit && results.Count > 0) return true;

                    if (distFromOrigin > maxLength)
                        return true;

                    if ((proxy.Fixture.CollisionLayer & ray.CollisionMask) == 0x0)
                    {
                        return true;
                    }

                    if (predicate?.Invoke(proxy.Fixture.Body.Owner) == true)
                    {
                        return true;
                    }

                    // TODO: Shape raycast here

                    // Need to convert it back to world-space.
                    var result = new RayCastResults(distFromOrigin, matrix.Transform(point), proxy.Fixture.Body.Owner);
                    results.Add(result);
#if DEBUG
                    EntityManager.EventBus.QueueEvent(EventSource.Local,
                        new DebugDrawRayMessage(
                            new DebugRayData(ray, maxLength, result)));
#endif
                    return true;
                }, gridRay);
            }

#if DEBUG
            if (results.Count == 0)
            {
                EntityManager.EventBus.QueueEvent(EventSource.Local,
                    new DebugDrawRayMessage(
                        new DebugRayData(ray, maxLength, null)));
            }
#endif

            results.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return results;
        }

        /// <summary>
        ///     Casts a ray in the world and returns the first entity it hits, or a list of all entities it hits.
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <param name="ignoredEnt">A single entity that can be ignored by the RayCast. Useful if the ray starts inside the body of an entity.</param>
        /// <param name="returnOnFirstHit">If false, will return a list of everything it hits, otherwise will just return a list of the first entity hit</param>
        /// <returns>An enumerable of either the first entity hit or everything hit</returns>
        public IEnumerable<RayCastResults> IntersectRay(MapId mapId, CollisionRay ray, float maxLength = 50, IEntity? ignoredEnt = null, bool returnOnFirstHit = true)
            => IntersectRayWithPredicate(mapId, ray, maxLength, entity => entity == ignoredEnt, returnOnFirstHit);

        /// <summary>
        ///     Casts a ray in the world and returns the distance the ray traveled while colliding with entities
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <param name="ignoredEnt">A single entity that can be ignored by the RayCast. Useful if the ray starts inside the body of an entity.</param>
        /// <returns>The distance the ray traveled while colliding with entities</returns>
        public float IntersectRayPenetration(MapId mapId, CollisionRay ray, float maxLength, IEntity? ignoredEnt = null)
        {
            var penetration = 0f;
            var endPoint = ray.Position + ray.Direction.Normalized * maxLength;
            var rayBox = new Box2(Vector2.ComponentMin(ray.Position, endPoint),
                Vector2.ComponentMax(ray.Position, endPoint));

            foreach (var broadphase in _broadphaseSystem.GetBroadphases(mapId, rayBox))
            {
                var offset = broadphase.Owner.Transform.InvWorldMatrix.Transform(ray.Position);
                var gridRot = new Angle(-broadphase.Owner.Transform.WorldRotation.Theta);
                var direction = gridRot.RotateVec(ray.Direction);

                var gridRay = new CollisionRay(offset, direction, ray.CollisionMask);

                broadphase.Tree.QueryRay((in FixtureProxy proxy, in Vector2 point, float distFromOrigin) =>
                {
                    if (distFromOrigin > maxLength || proxy.Fixture.Body.Owner == ignoredEnt) return true;

                    if ((proxy.Fixture.CollisionLayer & ray.CollisionMask) == 0x0)
                    {
                        return true;
                    }

                    if (new Ray(point + ray.Direction * proxy.AABB.Size.Length * 2, -ray.Direction).Intersects(
                        proxy.AABB, out _, out var exitPoint))
                    {
                        penetration += (point - exitPoint).Length;
                    }
                    return true;
                }, gridRay);
            }

#if DEBUG
            if (penetration > 0f)
            {
                EntityManager.EventBus.QueueEvent(EventSource.Local,
                    new DebugDrawRayMessage(
                        new DebugRayData(ray, maxLength, null)));
            }
#endif

            return penetration;
        }

        #endregion
    }
}
