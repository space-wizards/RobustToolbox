using System;
using System.Collections.Generic;
using Robust.Shared.Debugging;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems
{
    /*
     * Handles all of the public query methods for physics.
     */
    public partial class SharedPhysicsSystem
    {
        [Dependency] private readonly SharedDebugRayDrawingSystem _sharedDebugRaySystem = default!;

        /// <summary>
        ///     Get the percentage that 2 bodies overlap. Ignores whether collision is turned on for either body.
        /// </summary>
        /// <param name="bodyA"></param>
        /// <param name="bodyB"></param>
        /// <returns> 0 -> 1.0f based on WorldAABB overlap</returns>
        [Obsolete]
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

            foreach (var broadphase in _broadphase.GetBroadphases(mapId, collider))
            {
                var gridCollider = EntityManager.GetComponent<TransformComponent>(broadphase.Owner).InvWorldMatrix.TransformBox(collider);

                broadphase.StaticTree.QueryAabb(ref state, (ref (Box2 collider, MapId map, bool found) state, in FixtureProxy proxy) =>
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

                broadphase.DynamicTree.QueryAabb(ref state, (ref (Box2 collider, MapId map, bool found) state, in FixtureProxy proxy) =>
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

        /// <summary>
        ///     Get all the entities whose fixtures intersect the fixtures of the given entity. Basically a variant of
        ///     <see cref="GetCollidingEntities(PhysicsComponent, Vector2, bool)"/> that allows the user to specify
        ///     their own collision mask.
        /// </summary>
        public HashSet<EntityUid> GetEntitiesIntersectingBody(
            EntityUid uid,
            int collisionMask,
            bool approximate = true,
            PhysicsComponent? body = null,
            FixturesComponent? fixtureComp = null,
            TransformComponent? xform = null)
        {
            var entities = new HashSet<EntityUid>();

            if (!Resolve(uid, ref body, ref fixtureComp, ref xform, false))
                return entities;

            if (!_lookup.TryGetCurrentBroadphase(xform, out var broadphase))
                return entities;

            var state = (body, entities);

            foreach (var (_, fixture) in fixtureComp.Fixtures)
            {
                foreach (var proxy in fixture.Proxies)
                {
                    broadphase.StaticTree.QueryAabb(ref state,
                        (ref (PhysicsComponent body, HashSet<EntityUid> entities) state,
                            in FixtureProxy other) =>
                        {
                            if (other.Fixture.Body.Deleted || other.Fixture.Body == body) return true;
                            if ((collisionMask & other.Fixture.CollisionLayer) == 0x0) return true;

                            state.entities.Add(other.Fixture.Body.Owner);
                            return true;
                        }, proxy.AABB, approximate);

                    broadphase.DynamicTree.QueryAabb(ref state,
                        (ref (PhysicsComponent body, HashSet<EntityUid> entities) state,
                            in FixtureProxy other) =>
                        {
                            if (other.Fixture.Body.Deleted || other.Fixture.Body == body) return true;
                            if ((collisionMask & other.Fixture.CollisionLayer) == 0x0) return true;

                            state.entities.Add(other.Fixture.Body.Owner);
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

            foreach (var broadphase in _broadphase.GetBroadphases(mapId, worldAABB))
            {
                var gridAABB = EntityManager.GetComponent<TransformComponent>(broadphase.Owner).InvWorldMatrix.TransformBox(worldAABB);

                foreach (var proxy in broadphase.StaticTree.QueryAabb(gridAABB, false))
                {
                    bodies.Add(proxy.Fixture.Body);
                }

                foreach (var proxy in broadphase.DynamicTree.QueryAabb(gridAABB, false))
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

            foreach (var broadphase in _broadphase.GetBroadphases(mapId, worldBounds.CalcBoundingBox()))
            {
                var gridAABB = EntityManager.GetComponent<TransformComponent>(broadphase.Owner).InvWorldMatrix.TransformBox(worldBounds);

                foreach (var proxy in broadphase.StaticTree.QueryAabb(gridAABB, false))
                {
                    bodies.Add(proxy.Fixture.Body);
                }

                foreach (var proxy in broadphase.DynamicTree.QueryAabb(gridAABB, false))
                {
                    bodies.Add(proxy.Fixture.Body);
                }
            }

            return bodies;
        }

        public HashSet<PhysicsComponent> GetContactingEntities(PhysicsComponent body, bool approximate = false)
        {
            // HashSet to ensure that we only return each entity once, instead of once per colliding fixture.
            var result = new HashSet<PhysicsComponent>();
            var node = body.Contacts.First;

            while (node != null)
            {
                var contact = node.Value;
                node = node.Next;

                if (!approximate && !contact.IsTouching)
                    continue;

                var bodyA = contact.FixtureA!.Body;
                var bodyB = contact.FixtureB!.Body;

                result.Add(body == bodyA ? bodyB : bodyA);
            }

            return result;
        }

        /// <summary>
        ///     Checks whether a body is colliding
        /// </summary>
        public bool IsInContact(PhysicsComponent body, bool approximate = false)
        {
            var node = body.Contacts.First;

            while (node != null)
            {
                if (approximate || node.Value.IsTouching)
                    return true;

                node = node.Next;
            }

            return false;
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
        // TODO: Make the parameter order here consistent with the other overload.
        public IEnumerable<RayCastResults> IntersectRayWithPredicate(MapId mapId, CollisionRay ray,
            float maxLength = 50F, Func<EntityUid, bool>? predicate = null, bool returnOnFirstHit = true)
        {
            // No, rider. This is better than a local function!
            // ReSharper disable once ConvertToLocalFunction
            var wrapper =
                (EntityUid uid, Func<EntityUid, bool>? wrapped)
                    => wrapped != null && wrapped(uid);

            return IntersectRayWithPredicate(mapId, ray, predicate, wrapper, maxLength, returnOnFirstHit);
        }

        /// <summary>
        ///     Casts a ray in the world, returning the first entity it hits (or all entities it hits, if so specified)
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <param name="state">A custom state to pass to the predicate.</param>
        /// <param name="predicate">A predicate to check whether to ignore an entity or not. If it returns true, it will be ignored.</param>
        /// <param name="returnOnFirstHit">If true, will only include the first hit entity in results. Otherwise, returns all of them.</param>
        /// <remarks>You can avoid variable capture in many cases by using this method and passing a custom state to the predicate.</remarks>
        /// <returns>A result object describing the hit, if any.</returns>
        public IEnumerable<RayCastResults> IntersectRayWithPredicate<TState>(MapId mapId, CollisionRay ray, TState state,
            Func<EntityUid, TState, bool> predicate, float maxLength = 50F, bool returnOnFirstHit = true)
        {
            List<RayCastResults> results = new();
            var endPoint = ray.Position + ray.Direction.Normalized * maxLength;
            var rayBox = new Box2(Vector2.ComponentMin(ray.Position, endPoint),
                Vector2.ComponentMax(ray.Position, endPoint));

            foreach (var broadphase in _broadphase.GetBroadphases(mapId, rayBox))
            {
                var (_, rot, matrix, invMatrix) = Transform(broadphase.Owner).GetWorldPositionRotationMatrixWithInv();

                var position = invMatrix.Transform(ray.Position);
                var gridRot = new Angle(-rot.Theta);
                var direction = gridRot.RotateVec(ray.Direction);

                var gridRay = new CollisionRay(position, direction, ray.CollisionMask);

                broadphase.StaticTree.QueryRay((in FixtureProxy proxy, in Vector2 point, float distFromOrigin) =>
                {
                    if (returnOnFirstHit && results.Count > 0)
                        return true;

                    if (distFromOrigin > maxLength)
                        return true;

                    if ((proxy.Fixture.CollisionLayer & ray.CollisionMask) == 0x0)
                        return true;

                    if (!proxy.Fixture.Body.Hard)
                        return true;

                    if (predicate.Invoke(proxy.Fixture.Body.Owner, state) == true)
                        return true;

                    // TODO: Shape raycast here

                    // Need to convert it back to world-space.
                    var result = new RayCastResults(distFromOrigin, matrix.Transform(point), proxy.Fixture.Body.Owner);
                    results.Add(result);
                    _sharedDebugRaySystem.ReceiveLocalRayFromAnyThread(new DebugRayData(ray, maxLength, result));
                    return true;
                }, gridRay);

                broadphase.DynamicTree.QueryRay((in FixtureProxy proxy, in Vector2 point, float distFromOrigin) =>
                {
                    if (returnOnFirstHit && results.Count > 0)
                        return true;

                    if (distFromOrigin > maxLength)
                        return true;

                    if ((proxy.Fixture.CollisionLayer & ray.CollisionMask) == 0x0)
                        return true;

                    if (!proxy.Fixture.Body.Hard)
                        return true;

                    if (predicate.Invoke(proxy.Fixture.Body.Owner, state) == true)
                        return true;

                    // TODO: Shape raycast here

                    // Need to convert it back to world-space.
                    var result = new RayCastResults(distFromOrigin, matrix.Transform(point), proxy.Fixture.Body.Owner);
                    results.Add(result);
                    _sharedDebugRaySystem.ReceiveLocalRayFromAnyThread(new DebugRayData(ray, maxLength, result));
                    return true;
                }, gridRay);
            }

            if (results.Count == 0)
            {
                _sharedDebugRaySystem.ReceiveLocalRayFromAnyThread(new DebugRayData(ray, maxLength, null));
            }

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
        public IEnumerable<RayCastResults> IntersectRay(MapId mapId, CollisionRay ray, float maxLength = 50, EntityUid? ignoredEnt = null, bool returnOnFirstHit = true)
        {
            // ReSharper disable once ConvertToLocalFunction
            var wrapper = static (EntityUid uid, EntityUid? ignored)
                => uid == ignored;

            return IntersectRayWithPredicate(mapId, ray, ignoredEnt, wrapper, maxLength, returnOnFirstHit);
        }

        /// <summary>
        ///     Casts a ray in the world and returns the distance the ray traveled while colliding with entities
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <param name="ignoredEnt">A single entity that can be ignored by the RayCast. Useful if the ray starts inside the body of an entity.</param>
        /// <returns>The distance the ray traveled while colliding with entities</returns>
        public float IntersectRayPenetration(MapId mapId, CollisionRay ray, float maxLength, EntityUid? ignoredEnt = null)
        {
            var penetration = 0f;
            var endPoint = ray.Position + ray.Direction.Normalized * maxLength;
            var rayBox = new Box2(Vector2.ComponentMin(ray.Position, endPoint),
                Vector2.ComponentMax(ray.Position, endPoint));

            foreach (var broadphase in _broadphase.GetBroadphases(mapId, rayBox))
            {
                var (_, rot, invMatrix) = Transform(broadphase.Owner).GetWorldPositionRotationInvMatrix();

                var position = invMatrix.Transform(ray.Position);
                var gridRot = new Angle(-rot.Theta);
                var direction = gridRot.RotateVec(ray.Direction);

                var gridRay = new CollisionRay(position, direction, ray.CollisionMask);

                broadphase.StaticTree.QueryRay((in FixtureProxy proxy, in Vector2 point, float distFromOrigin) =>
                {
                    if (distFromOrigin > maxLength || proxy.Fixture.Body.Owner == ignoredEnt)
                        return true;

                    if (!proxy.Fixture.Hard)
                        return true;

                    if ((proxy.Fixture.CollisionLayer & ray.CollisionMask) == 0x0)
                        return true;

                    if (new Ray(point + gridRay.Direction * proxy.AABB.Size.Length * 2, -gridRay.Direction).Intersects(
                            proxy.AABB, out _, out var exitPoint))
                    {
                        penetration += (point - exitPoint).Length;
                    }
                    return true;
                }, gridRay);

                broadphase.DynamicTree.QueryRay((in FixtureProxy proxy, in Vector2 point, float distFromOrigin) =>
                {
                    if (distFromOrigin > maxLength || proxy.Fixture.Body.Owner == ignoredEnt)
                        return true;

                    if (!proxy.Fixture.Hard)
                        return true;

                    if ((proxy.Fixture.CollisionLayer & ray.CollisionMask) == 0x0)
                        return true;

                    if (new Ray(point + gridRay.Direction * proxy.AABB.Size.Length * 2, -gridRay.Direction).Intersects(
                            proxy.AABB, out _, out var exitPoint))
                    {
                        penetration += (point - exitPoint).Length;
                    }
                    return true;
                }, gridRay);
            }

            // This hid rays that didn't penetrate something. Don't hide those because that causes rays to disappear that shouldn't.
            _sharedDebugRaySystem.ReceiveLocalRayFromAnyThread(new DebugRayData(ray, maxLength, null));

            return penetration;
        }

        #endregion

        #region Distance

        /// <summary>
        /// Gets the nearest distance of 2 entities, ignoring any sensor proxies.
        /// </summary>
        public bool TryGetDistance(EntityUid uidA, EntityUid uidB,
            out float distance,
            TransformComponent? xformA = null, TransformComponent? xformB = null,
            FixturesComponent? managerA = null, FixturesComponent? managerB = null,
            PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)
        {
            return TryGetNearest(uidA, uidB, out _, out _, out distance, xformA, xformB, managerA, managerB, bodyA, bodyB);
        }

        /// <summary>
        /// Get the nearest non-sensor points on entity A and entity B to each other.
        /// </summary>
        public bool TryGetNearestPoints(EntityUid uidA, EntityUid uidB,
            out Vector2 pointA, out Vector2 pointB,
            TransformComponent? xformA = null, TransformComponent? xformB = null,
            FixturesComponent? managerA = null, FixturesComponent? managerB = null,
            PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)
        {
            return TryGetNearest(uidA, uidB, out pointA, out pointB, out _, xformA, xformB, managerA, managerB, bodyA, bodyB);
        }

        public bool TryGetNearest(EntityUid uidA, EntityUid uidB,
            out Vector2 pointA,
            out Vector2 pointB,
            out float distance,
            Transform xfA, Transform xfB,
            FixturesComponent? managerA = null, FixturesComponent? managerB = null,
            PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)
        {
            pointA = Vector2.Zero;
            pointB = Vector2.Zero;

            if (!Resolve(uidA, ref managerA, ref bodyA) ||
                !Resolve(uidB, ref managerB,  ref bodyB) ||
                managerA.FixtureCount == 0 ||
                managerB.FixtureCount == 0)
            {
                distance = 0f;
                return false;
            }

            distance = float.MaxValue;
            var input = new DistanceInput();

            input.TransformA = xfA;
            input.TransformB = xfB;
            input.UseRadii = true;

            // No requirement on collision being enabled so chainshapes will fail
            foreach (var (_, fixtureA) in managerA.Fixtures)
            {
                if (bodyA.Hard && !fixtureA.Hard)
                    continue;

                DebugTools.Assert(fixtureA.ProxyCount <= 1);

                foreach (var (_, fixtureB) in managerB.Fixtures)
                {
                    if (bodyB.Hard && !fixtureB.Hard)
                        continue;

                    DebugTools.Assert(fixtureB.ProxyCount <= 1);
                    input.ProxyA.Set(fixtureA.Shape, 0);
                    input.ProxyB.Set(fixtureB.Shape, 0);
                    DistanceManager.ComputeDistance(out var output, out _, input);

                    if (distance < output.Distance)
                        continue;

                    pointA = output.PointA;
                    pointB = output.PointB;
                    distance = output.Distance;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the nearest points in map terms and the distance between them.
        /// If a body is hard it only considers hard fixtures.
        /// </summary>
        public bool TryGetNearest(EntityUid uidA, EntityUid uidB,
            out Vector2 pointA,
            out Vector2 pointB,
            out float distance,
            TransformComponent? xformA = null, TransformComponent? xformB = null,
            FixturesComponent? managerA = null, FixturesComponent? managerB = null,
            PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)
        {
            if (!Resolve(uidA, ref xformA) || !Resolve(uidB, ref xformB) ||
                xformA.MapID != xformB.MapID)
            {
                pointA = Vector2.Zero;
                pointB = Vector2.Zero;
                distance = 0f;
                return false;
            }

            var xformQuery = GetEntityQuery<TransformComponent>();
            var xfA = GetPhysicsTransform(uidA, xformA, xformQuery);
            var xfB = GetPhysicsTransform(uidB, xformB, xformQuery);

            return TryGetNearest(uidA, uidB, out pointA, out pointB, out distance, xfA, xfB);
        }

        #endregion
    }
}
