using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems
{
    public abstract class SharedBroadphaseSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;
        [Dependency] private readonly IParallelManager _parallel = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedGridTraversalSystem _traversal = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        private EntityQuery<BroadphaseComponent> _broadphaseQuery;
        private EntityQuery<MapGridComponent> _gridQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;
        private EntityQuery<TransformComponent> _xformQuery;
        private EntityQuery<PhysicsMapComponent> _mapQuery;

        /*
         * Okay so Box2D has its own "MoveProxy" stuff so you can easily find new contacts when required.
         * Our problem is that we have nested broadphases (rather than being on separate maps) which makes this
         * not feasible because a body could be intersecting 2 broadphases.
         * Hence we need to check which broadphases it does intersect and checkar for colliding bodies.
         */

        /// <summary>
        /// How much to expand bounds by to check cross-broadphase collisions.
        /// Ideally you want to set this to your largest body size.
        /// This only has a noticeable performance impact where multiple broadphases are in close proximity.
        /// </summary>
        private float _broadphaseExpand;

        private const int PairBufferParallel = 8;

        private ObjectPool<List<FixtureProxy>> _bufferPool =
            new DefaultObjectPool<List<FixtureProxy>>(new ListPolicy<FixtureProxy>(), 2048);

        public override void Initialize()
        {
            base.Initialize();

            _broadphaseQuery = GetEntityQuery<BroadphaseComponent>();
            _gridQuery = GetEntityQuery<MapGridComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();
            _mapQuery = GetEntityQuery<PhysicsMapComponent>();

            UpdatesOutsidePrediction = true;
            UpdatesAfter.Add(typeof(SharedTransformSystem));

            Subs.CVar(_cfg, CVars.BroadphaseExpand, SetBroadphaseExpand, true);
        }

        private void SetBroadphaseExpand(float value) => _broadphaseExpand = value;

        #region Find Contacts

        /// <summary>
        /// Check the AABB for each moved broadphase fixture and add any colliding entities to the movebuffer in case.
        /// </summary>
        private void FindGridContacts(
            PhysicsMapComponent component,
            MapId mapId,
            HashSet<EntityUid> movedGrids,
            Dictionary<FixtureProxy, Box2> gridMoveBuffer)
        {
            // None moved this tick
            if (movedGrids.Count == 0) return;

            var mapBroadphase = _broadphaseQuery.GetComponent(_mapManager.GetMapEntityId(mapId));

            // This is so that if we're on a broadphase that's moving (e.g. a grid) we need to make sure anything
            // we move over is getting checked for collisions, and putting it on the movebuffer is the easiest way to do so.
            var moveBuffer = component.MoveBuffer;

            foreach (var gridUid in movedGrids)
            {
                var grid = _gridQuery.GetComponent(gridUid);
                var xform = _xformQuery.GetComponent(gridUid);

                DebugTools.Assert(xform.MapID == mapId);
                var worldAABB = _transform.GetWorldMatrix(xform).TransformBox(grid.LocalAABB);
                var enlargedAABB = worldAABB.Enlarged(_broadphaseExpand);
                var state = (moveBuffer, gridMoveBuffer);

                QueryMapBroadphase(mapBroadphase.DynamicTree, ref state, enlargedAABB);
                QueryMapBroadphase(mapBroadphase.StaticTree, ref state, enlargedAABB);
            }

            foreach (var (proxy, worldAABB) in gridMoveBuffer)
            {
                moveBuffer[proxy] = worldAABB;
                // If something is in our AABB then try grid traversal for it
                _traversal.CheckTraverse(proxy.Entity, Transform(proxy.Entity));
            }
        }

        private void QueryMapBroadphase(IBroadPhase broadPhase,
            ref (Dictionary<FixtureProxy, Box2>, Dictionary<FixtureProxy, Box2>) state,
            Box2 enlargedAABB)
        {
            // Easier to just not go over each proxy as we already unioned the fixture's worldaabb.
            broadPhase.QueryAabb(ref state, static (ref (
                    Dictionary<FixtureProxy, Box2> moveBuffer,
                    Dictionary<FixtureProxy, Box2> gridMoveBuffer) tuple,
                in FixtureProxy value) =>
            {
                // 99% of the time it's just going to be the broadphase (for now the grid) itself.
                // hence this body check makes this run significantly better.
                // Also check if it's not already on the movebuffer.
                if (tuple.moveBuffer.ContainsKey(value))
                    return true;

                // To avoid updating during iteration.
                // Don't need to transform as it's already in map terms.
                tuple.gridMoveBuffer[value] = value.AABB;
                return true;
            }, enlargedAABB, true);
        }

        [Obsolete("Use the overload with SharedPhysicsMapComponent")]
        internal void FindNewContacts(MapId mapId)
        {
            if (!TryComp<PhysicsMapComponent>(_mapManager.GetMapEntityId(mapId), out var physicsMap))
                return;

            FindNewContacts(physicsMap, mapId);
        }

        /// <summary>
        /// Go through every single created, moved, or touched proxy on the map and try to find any new contacts that should be created.
        /// </summary>
        internal void FindNewContacts(PhysicsMapComponent component, MapId mapId)
        {
            var moveBuffer = component.MoveBuffer;
            var mapUid = _mapManager.GetMapEntityId(mapId);
            var movedGrids = Comp<MovedGridsComponent>(mapUid).MovedGrids;
            var gridMoveBuffer = new Dictionary<FixtureProxy, Box2>();

            // Find any entities being driven over that might need to be considered
            FindGridContacts(component, mapId, movedGrids, gridMoveBuffer);

            // There is some mariana trench levels of bullshit going on.
            // We essentially need to re-create Box2D's FindNewContacts but in a way that allows us to check every
            // broadphase intersecting a particular proxy instead of just on the 1 broadphase.
            // This means we can generate contacts across different broadphases.
            // If you have a better way of allowing for broadphases attached to grids then by all means code it yourself.

            // FindNewContacts is inherently going to be a lot slower than Box2D's normal version so we need
            // to cache a bunch of stuff to make up for it.

            // Handle grids first as they're not stored on map broadphase at all.
            HandleGridCollisions(mapId, movedGrids);

            // EZ
            if (moveBuffer.Count == 0)
                return;

            var count = moveBuffer.Count;
            var contactBuffer = ArrayPool<List<FixtureProxy>>.Shared.Rent(count);
            var pMoveBuffer = ArrayPool<(FixtureProxy Proxy, Box2 AABB)>.Shared.Rent(count);

            var idx = 0;

            foreach (var (proxy, aabb) in moveBuffer)
            {
                contactBuffer[idx] = _bufferPool.Get();
                pMoveBuffer[idx++] = (proxy, aabb);
            }

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _parallel.ParallelProcessCount,
            };

            var batches = (int)MathF.Ceiling((float) count / PairBufferParallel);

            Parallel.For(0, batches, options, i =>
            {
                var start = i * PairBufferParallel;
                var end = Math.Min(start + PairBufferParallel, count);

                for (var j = start; j < end; j++)
                {
                    var (proxy, worldAABB) = pMoveBuffer[j];
                    var buffer = contactBuffer[j];

                    var proxyBody = proxy.Body;
                    DebugTools.Assert(!proxyBody.Deleted);

                    var state = (this, proxy, worldAABB, buffer);

                    // Get every broadphase we may be intersecting.
                    _mapManager.FindGridsIntersecting(mapId, worldAABB.Enlarged(_broadphaseExpand), ref state,
                        static (EntityUid uid, MapGridComponent _, ref (
                            SharedBroadphaseSystem system,
                            FixtureProxy proxy,
                            Box2 worldAABB,
                            List<FixtureProxy> pairBuffer) tuple) =>
                        {
                            ref var buffer = ref tuple.pairBuffer;
                            tuple.system.FindPairs(tuple.proxy, tuple.worldAABB, uid, buffer);
                            return true;
                        }, approx: true, includeMap: false);

                    // Struct ref moment, I have no idea what's fastest.
                    buffer = state.buffer;
                    FindPairs(proxy, worldAABB, _mapManager.GetMapEntityId(mapId), buffer);
                }
            });

            for (var i = 0; i < count; i++)
            {
                var proxyA = pMoveBuffer[i].Proxy;
                var proxies = contactBuffer[i];
                var proxyABody = proxyA.Body;
                FixturesComponent? manager = null;

                foreach (var other in proxies)
                {
                    var otherBody = other.Body;

                    // Because we may be colliding with something asleep (due to the way grid movement works) need
                    // to make sure the contact doesn't fail.
                    // This is because we generate a contact across 2 different broadphases where both bodies aren't
                    // moving locally but are moving in world-terms.
                    if (proxyA.Fixture.Hard && other.Fixture.Hard &&
                        (gridMoveBuffer.ContainsKey(proxyA) || gridMoveBuffer.ContainsKey(other)))
                    {
                        _physicsSystem.WakeBody(proxyA.Entity, force: true, manager: manager, body: proxyABody);
                        _physicsSystem.WakeBody(other.Entity, force: true, body: otherBody);
                    }

                    _physicsSystem.AddPair(proxyA.FixtureId, other.FixtureId, proxyA, other);
                }

                _bufferPool.Return(contactBuffer[i]);
                pMoveBuffer[i] = default;
            }

            ArrayPool<List<FixtureProxy>>.Shared.Return(contactBuffer);
            ArrayPool<(FixtureProxy Proxy, Box2 AABB)>.Shared.Return(pMoveBuffer);
            moveBuffer.Clear();
            movedGrids.Clear();
        }

        private void HandleGridCollisions(
            MapId mapId,
            HashSet<EntityUid> movedGrids)
        {
            foreach (var gridUid in movedGrids)
            {
                var grid = _gridQuery.GetComponent(gridUid);
                var xform = _xformQuery.GetComponent(gridUid);
                DebugTools.Assert(xform.MapID == mapId);

                var (worldPos, worldRot, worldMatrix, invWorldMatrix) = _transform.GetWorldPositionRotationMatrixWithInv(xform);

                var aabb = new Box2Rotated(grid.LocalAABB, worldRot).CalcBoundingBox().Translated(worldPos);

                // TODO: Need to handle grids colliding with non-grid entities with the same layer
                // (nothing in SS14 does this yet).

                var transform = _physicsSystem.GetPhysicsTransform(gridUid);
                var state = (gridUid, grid, transform, worldMatrix, invWorldMatrix, _map, _physicsSystem, _transform, _physicsQuery, _xformQuery);

                _mapManager.FindGridsIntersecting(mapId, aabb, ref state,
                    static (EntityUid uid, MapGridComponent component,
                        ref (EntityUid gridUid,
                            MapGridComponent grid,
                            Transform transform,
                            Matrix3 worldMatrix,
                            Matrix3 invWorldMatrix,
                            SharedMapSystem _map,
                            SharedPhysicsSystem _physicsSystem,
                            SharedTransformSystem xformSystem,
                            EntityQuery<PhysicsComponent> physicsQuery,
                            EntityQuery<TransformComponent> xformQuery) tuple) =>
                    {
                        if (tuple.gridUid == uid ||
                        !tuple.xformQuery.TryGetComponent(uid, out var collidingXform))
                        {
                            return true;
                        }

                        var (_, _, otherGridMatrix, otherGridInvMatrix) =  tuple.xformSystem.GetWorldPositionRotationMatrixWithInv(collidingXform, tuple.xformQuery);
                        var otherGridBounds = otherGridMatrix.TransformBox(component.LocalAABB);
                        var otherTransform = tuple._physicsSystem.GetPhysicsTransform(uid);

                        // Get Grid2 AABB in grid1 ref
                        var aabb1 = tuple.grid.LocalAABB.Intersect(tuple.invWorldMatrix.TransformBox(otherGridBounds));

                        // TODO: AddPair has a nasty check in there that's O(n) but that's also a general physics problem.
                        var ourChunks = tuple._map.GetLocalMapChunks(tuple.gridUid, tuple.grid, aabb1);
                        var physicsA = tuple.physicsQuery.GetComponent(tuple.gridUid);
                        var physicsB = tuple.physicsQuery.GetComponent(uid);

                        // Only care about chunks on other grid overlapping us.
                        while (ourChunks.MoveNext(out var ourChunk))
                        {
                            var ourChunkWorld =
                                tuple.worldMatrix.TransformBox(
                                    ourChunk.CachedBounds.Translated(ourChunk.Indices * tuple.grid.ChunkSize));
                            var ourChunkOtherRef = otherGridInvMatrix.TransformBox(ourChunkWorld);
                            var collidingChunks = tuple._map.GetLocalMapChunks(uid, component, ourChunkOtherRef);

                            while (collidingChunks.MoveNext(out var collidingChunk))
                            {
                                foreach (var (ourId, fixture) in ourChunk.Fixtures)
                                {
                                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                                    {
                                        var fixAABB = fixture.Shape.ComputeAABB(tuple.transform, i);

                                        foreach (var (otherId, otherFixture) in collidingChunk.Fixtures)
                                        {
                                            for (var j = 0; j < otherFixture.Shape.ChildCount; j++)
                                            {
                                                var otherAABB = otherFixture.Shape.ComputeAABB(otherTransform, j);

                                                if (!fixAABB.Intersects(otherAABB)) continue;
                                                tuple._physicsSystem.AddPair(tuple.gridUid, uid,
                                                    ourId, otherId,
                                                    fixture, i,
                                                    otherFixture, j,
                                                    physicsA, physicsB,
                                                    ContactFlags.Grid);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return true;
                    }, approx: true, includeMap: false);
            }
        }

        #endregion

        private void FindPairs(
            FixtureProxy proxy,
            Box2 worldAABB,
            EntityUid broadphase,
            List<FixtureProxy> pairBuffer)
        {
            DebugTools.Assert(proxy.Body.CanCollide);

            // Broadphase can't intersect with entities on itself so skip.
            if (proxy.Entity == broadphase || !_xformQuery.TryGetComponent(proxy.Entity, out var xform))
            {
                return;
            }

            // Logger.DebugS("physics", $"Checking proxy for {proxy.Entity} on {broadphase.Owner}");
            Box2 aabb;
            DebugTools.AssertNotNull(xform.Broadphase);
            if (!_lookup.TryGetCurrentBroadphase(xform, out var proxyBroad))
            {
                Log.Error($"Found null broadphase for {ToPrettyString(proxy.Entity)}");
                DebugTools.Assert(false);
                return;
            }

            // If it's the same broadphase as our body's one then don't need to translate the AABB.
            if (proxyBroad.Owner == broadphase)
            {
                aabb = proxy.AABB;
            }
            else
            {
                aabb = _transform.GetInvWorldMatrix(broadphase).TransformBox(worldAABB);
            }

            var broadphaseComp = _broadphaseQuery.GetComponent(broadphase);
            var state = (pairBuffer, proxy);

            QueryBroadphase(broadphaseComp.DynamicTree, state, aabb);

            if ((proxy.Body.BodyType & BodyType.Static) != 0x0)
                return;

            QueryBroadphase(broadphaseComp.StaticTree, state, aabb);
        }

        private void QueryBroadphase(IBroadPhase broadPhase, (List<FixtureProxy>, FixtureProxy) state, Box2 aabb)
        {
            broadPhase.QueryAabb(ref state, static (
                ref (List<FixtureProxy> pairBuffer, FixtureProxy proxy) tuple,
                in FixtureProxy other) =>
            {
                DebugTools.Assert(other.Body.CanCollide);
                // Logger.DebugS("physics", $"Checking {proxy.Entity} against {other.Fixture.Body.Owner} at {aabb}");

                if (tuple.proxy == other ||
                    !SharedPhysicsSystem.ShouldCollide(tuple.proxy.Fixture, other.Fixture) ||
                    tuple.proxy.Entity == other.Entity)
                {
                    return true;
                }

                tuple.pairBuffer.Add(other);
                return true;
            }, aabb, true);
        }

        public void RegenerateContacts(EntityUid uid, PhysicsComponent body, FixturesComponent? fixtures = null, TransformComponent? xform = null)
        {
            _physicsSystem.DestroyContacts(body);
            if (!Resolve(uid, ref xform, ref fixtures))
                return;

            if (xform.MapUid == null)
                return;

            if (!_xformQuery.TryGetComponent(xform.Broadphase?.Uid, out var broadphase))
                return;

            _physicsSystem.SetAwake((uid, body), true);

            var matrix = _transform.GetWorldMatrix(broadphase);
            foreach (var fixture in fixtures.Fixtures.Values)
            {
                TouchProxies(xform.MapUid.Value, matrix, fixture);
            }
        }

        private void TouchProxies(EntityUid mapId, Matrix3 broadphaseMatrix, Fixture fixture)
        {
            foreach (var proxy in fixture.Proxies)
            {
                AddToMoveBuffer(mapId, proxy, broadphaseMatrix.TransformBox(proxy.AABB));
            }
        }

        private void AddToMoveBuffer(EntityUid mapId, FixtureProxy proxy, Box2 aabb)
        {
            if (!_mapQuery.TryGetComponent(mapId, out var physicsMap))
                return;

            DebugTools.Assert(proxy.Body.CanCollide);

            physicsMap.MoveBuffer[proxy] = aabb;
        }

        public void Refilter(EntityUid uid, Fixture fixture, TransformComponent? xform = null)
        {
            foreach (var contact in fixture.Contacts.Values)
            {
                contact.Flags |= ContactFlags.Filter;
            }

            if (!Resolve(uid, ref xform))
                return;

            if (xform.MapUid == null)
                return;

            if (!_xformQuery.TryGetComponent(xform.Broadphase?.Uid, out var broadphase))
                return;

            var matrix = _transform.GetWorldMatrix(broadphase);
            TouchProxies(xform.MapUid.Value, matrix, fixture);
        }

        // TODO: The below is slow and should just query the map's broadphase directly. The problem is that
        // there's some ordering stuff going on where the broadphase has queued all of its updates but hasn't applied
        // them yet so this query will fail on initialization which chains into a whole lot of issues.
        internal IEnumerable<(EntityUid uid, BroadphaseComponent comp)> GetBroadphases(MapId mapId, Box2 aabb)
        {
            // TODO Okay so problem: If we just do Encloses that's a lot faster BUT it also means we don't return the
            // map's broadphase which avoids us iterating over it for 99% of bodies.

            if (mapId == MapId.Nullspace) yield break;

            var enumerator = AllEntityQuery<BroadphaseComponent, TransformComponent>();

            while (enumerator.MoveNext(out var bUid, out var broadphase, out var xform))
            {
                if (xform.MapID != mapId) continue;

                if (!EntityManager.TryGetComponent(bUid, out MapGridComponent? mapGrid))
                {
                    yield return (bUid, broadphase);
                    continue;
                }

                // Won't worry about accurate bounds checks as it's probably slower in most use cases.
                var chunkEnumerator = _map.GetMapChunks(bUid, mapGrid, aabb);

                if (chunkEnumerator.MoveNext(out _))
                {
                    yield return (bUid, broadphase);
                }
            }
        }
    }
}
