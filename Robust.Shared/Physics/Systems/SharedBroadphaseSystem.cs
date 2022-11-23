using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Collections;
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
using SharpZstd.Interop;

namespace Robust.Shared.Physics.Systems
{
    public abstract class SharedBroadphaseSystem : EntitySystem
    {
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;
        [Dependency] private readonly IParallelManager _parallel = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;

        private ISawmill _logger = default!;

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

            _logger = Logger.GetSawmill("physics");
            UpdatesOutsidePrediction = true;

            UpdatesAfter.Add(typeof(SharedTransformSystem));

            _cfg.OnValueChanged(CVars.BroadphaseExpand, SetBroadphaseExpand, true);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _cfg.UnsubValueChanged(CVars.BroadphaseExpand, SetBroadphaseExpand);
        }

        private void SetBroadphaseExpand(float value) => _broadphaseExpand = value;

        #region Find Contacts

        /// <summary>
        /// Check the AABB for each moved broadphase fixture and add any colliding entities to the movebuffer in case.
        /// </summary>
        private void FindGridContacts(
            SharedPhysicsMapComponent component,
            MapId mapId,
            HashSet<MapGridComponent> movedGrids,
            Dictionary<FixtureProxy, Box2> gridMoveBuffer,
            EntityQuery<BroadphaseComponent> broadQuery,
            EntityQuery<TransformComponent> xformQuery)
        {
            // None moved this tick
            if (movedGrids.Count == 0) return;

            var mapBroadphase = broadQuery.GetComponent(_mapManager.GetMapEntityId(mapId));

            // This is so that if we're on a broadphase that's moving (e.g. a grid) we need to make sure anything
            // we move over is getting checked for collisions, and putting it on the movebuffer is the easiest way to do so.
            var moveBuffer = component.MoveBuffer;

            foreach (var grid in movedGrids)
            {
                var xform = xformQuery.GetComponent(grid.GridEntityId);

                DebugTools.Assert(xform.MapID == mapId);
                var worldAABB = xform.WorldMatrix.TransformBox(grid.LocalAABB);
                var enlargedAABB = worldAABB.Enlarged(_broadphaseExpand);
                var state = (moveBuffer, gridMoveBuffer);

                QueryMapBroadphase(mapBroadphase.DynamicTree, ref state, enlargedAABB);
                QueryMapBroadphase(mapBroadphase.StaticTree, ref state, enlargedAABB);
            }

            foreach (var (proxy, worldAABB) in gridMoveBuffer)
            {
                moveBuffer[proxy] = worldAABB;
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
            if (!TryComp<SharedPhysicsMapComponent>(_mapManager.GetMapEntityId(mapId), out var physicsMap))
                return;

            FindNewContacts(physicsMap, mapId);
        }

        /// <summary>
        /// Go through every single created, moved, or touched proxy on the map and try to find any new contacts that should be created.
        /// </summary>
        internal void FindNewContacts(SharedPhysicsMapComponent component, MapId mapId)
        {
            var moveBuffer = component.MoveBuffer;
            var movedGrids = _mapManager.GetMovedGrids(mapId);
            var gridMoveBuffer = new Dictionary<FixtureProxy, Box2>();

            var broadphaseQuery = GetEntityQuery<BroadphaseComponent>();
            var physicsQuery = GetEntityQuery<PhysicsComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();

            // Find any entities being driven over that might need to be considered
            FindGridContacts(component, mapId, movedGrids, gridMoveBuffer, broadphaseQuery, xformQuery);

            // There is some mariana trench levels of bullshit going on.
            // We essentially need to re-create Box2D's FindNewContacts but in a way that allows us to check every
            // broadphase intersecting a particular proxy instead of just on the 1 broadphase.
            // This means we can generate contacts across different broadphases.
            // If you have a better way of allowing for broadphases attached to grids then by all means code it yourself.

            // FindNewContacts is inherently going to be a lot slower than Box2D's normal version so we need
            // to cache a bunch of stuff to make up for it.
            var contactManager = component.ContactManager;

            // Handle grids first as they're not stored on map broadphase at all.
            HandleGridCollisions(mapId, contactManager, movedGrids, physicsQuery, xformQuery);

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

            var options = new ParallelOptions()
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

                    var proxyBody = proxy.Fixture.Body;
                    DebugTools.Assert(!proxyBody.Deleted);

                    var state = (this, proxy, worldAABB, buffer, xformQuery, broadphaseQuery);

                    // Get every broadphase we may be intersecting.
                    _mapManager.FindGridsIntersectingApprox(mapId, worldAABB.Enlarged(_broadphaseExpand), ref state,
                        static (MapGridComponent grid, ref (
                            SharedBroadphaseSystem system,
                            FixtureProxy proxy,
                            Box2 worldAABB,
                            List<FixtureProxy> pairBuffer,
                            EntityQuery<TransformComponent> xformQuery,
                            EntityQuery<BroadphaseComponent> broadphaseQuery) tuple) =>
                        {
                            ref var buffer = ref tuple.pairBuffer;
                            tuple.system.FindPairs(tuple.proxy, tuple.worldAABB, grid.Owner, buffer, tuple.xformQuery, tuple.broadphaseQuery);
                            return true;
                        });

                    // Struct ref moment, I have no idea what's fastest.
                    buffer = state.buffer;
                    FindPairs(proxy, worldAABB, _mapManager.GetMapEntityId(mapId), buffer, xformQuery, broadphaseQuery);
                }
            });

            for (var i = 0; i < count; i++)
            {
                var proxyA = pMoveBuffer[i].Proxy;
                var proxies = contactBuffer[i];
                var proxyABody = proxyA.Fixture.Body;

                foreach (var other in proxies)
                {
                    var otherBody = other.Fixture.Body;
                    // Because we may be colliding with something asleep (due to the way grid movement works) need
                    // to make sure the contact doesn't fail.
                    // This is because we generate a contact across 2 different broadphases where both bodies aren't
                    // moving locally but are moving in world-terms.
                    if (proxyA.Fixture.Hard && other.Fixture.Hard &&
                        (gridMoveBuffer.ContainsKey(proxyA) || gridMoveBuffer.ContainsKey(other)))
                    {
                        _physicsSystem.WakeBody(proxyABody, force: true);
                        _physicsSystem.WakeBody(otherBody, force: true);
                    }

                    contactManager.AddPair(proxyA, other);
                }
            }

            for (var i = 0; i < count; i++)
            {
                _bufferPool.Return(contactBuffer[i]);
            }

            ArrayPool<List<FixtureProxy>>.Shared.Return(contactBuffer);
            ArrayPool<(FixtureProxy Proxy, Box2 AABB)>.Shared.Return(pMoveBuffer);
            moveBuffer.Clear();
            _mapManager.ClearMovedGrids(mapId);
        }

        private void HandleGridCollisions(
            MapId mapId,
            ContactManager contactManager,
            HashSet<MapGridComponent> movedGrids,
            EntityQuery<PhysicsComponent> bodyQuery,
            EntityQuery<TransformComponent> xformQuery)
        {
            var gridsPool = new List<MapGridComponent>();

            foreach (var grid in movedGrids)
            {
                var xform = xformQuery.GetComponent(grid.GridEntityId);
                DebugTools.Assert(xform.MapID == mapId);

                var (worldPos, worldRot, worldMatrix, invWorldMatrix) = xform.GetWorldPositionRotationMatrixWithInv(xformQuery);

                var aabb = new Box2Rotated(grid.LocalAABB, worldRot).CalcBoundingBox().Translated(worldPos);

                // TODO: Need to handle grids colliding with non-grid entities with the same layer
                // (nothing in SS14 does this yet).

                var transform = _physicsSystem.GetPhysicsTransform(grid.GridEntityId, xformQuery: xformQuery);
                gridsPool.Clear();

                foreach (var colliding in _mapManager.FindGridsIntersecting(mapId, aabb, gridsPool, xformQuery, bodyQuery, true))
                {
                    if (grid == colliding ||
                        !xformQuery.TryGetComponent(colliding.GridEntityId, out var collidingXform))
                    {
                        continue;
                    }

                    var (_, _, otherGridMatrix, otherGridInvMatrix) =  collidingXform.GetWorldPositionRotationMatrixWithInv();
                    var otherGridBounds = otherGridMatrix.TransformBox(colliding.LocalAABB);
                    var otherTransform = _physicsSystem.GetPhysicsTransform(colliding.GridEntityId, xformQuery: xformQuery);

                    // Get Grid2 AABB in grid1 ref
                    var aabb1 = grid.LocalAABB.Intersect(invWorldMatrix.TransformBox(otherGridBounds));

                    // TODO: AddPair has a nasty check in there that's O(n) but that's also a general physics problem.
                    var ourChunks = grid.GetLocalMapChunks(aabb1);

                    // Only care about chunks on other grid overlapping us.
                    while (ourChunks.MoveNext(out var ourChunk))
                    {
                        var ourChunkWorld = worldMatrix.TransformBox(ourChunk.CachedBounds.Translated(ourChunk.Indices * grid.ChunkSize));
                        var ourChunkOtherRef = otherGridInvMatrix.TransformBox(ourChunkWorld);
                        var collidingChunks = colliding.GetLocalMapChunks(ourChunkOtherRef);

                        while (collidingChunks.MoveNext(out var collidingChunk))
                        {
                            foreach (var fixture in ourChunk.Fixtures)
                            {
                                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                                {
                                    var fixAABB = fixture.Shape.ComputeAABB(transform, i);

                                    foreach (var otherFixture in collidingChunk.Fixtures)
                                    {
                                        for (var j = 0; j < otherFixture.Shape.ChildCount; j++)
                                        {
                                            var otherAABB = otherFixture.Shape.ComputeAABB(otherTransform, j);

                                            if (!fixAABB.Intersects(otherAABB)) continue;
                                            contactManager.AddPair(fixture, i, otherFixture, j, ContactFlags.Grid);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        private void FindPairs(
            FixtureProxy proxy,
            Box2 worldAABB,
            EntityUid broadphase,
            List<FixtureProxy> pairBuffer,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<BroadphaseComponent> broadphaseQuery)
        {
            DebugTools.Assert(proxy.Fixture.Body.CanCollide);

            var proxyBody = proxy.Fixture.Body;

            // Broadphase can't intersect with entities on itself so skip.
            if (proxyBody.Owner == broadphase || !xformQuery.TryGetComponent(proxyBody.Owner, out var xform))
            {
                return;
            }

            // Logger.DebugS("physics", $"Checking proxy for {proxy.Fixture.Body.Owner} on {broadphase.Owner}");
            Box2 aabb;
            if (!_lookup.TryGetCurrentBroadphase(xform, out var proxyBroad))
            {
                _logger.Error($"Found null broadphase for {ToPrettyString(proxy.Fixture.Body.Owner)}");
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
                var broadXform = xformQuery.GetComponent(broadphase);
                aabb = broadXform.InvWorldMatrix.TransformBox(worldAABB);
            }

            var broadphaseComp = broadphaseQuery.GetComponent(broadphase);
            var state = (pairBuffer, proxy);

            QueryBroadphase(broadphaseComp.DynamicTree, state, aabb);

            if ((proxy.Fixture.Body.BodyType & BodyType.Static) != 0x0)
                return;

            QueryBroadphase(broadphaseComp.StaticTree, state, aabb);
            pairBuffer = state.pairBuffer;
        }

        private void QueryBroadphase(IBroadPhase broadPhase, (List<FixtureProxy>, FixtureProxy) state, Box2 aabb)
        {
            broadPhase.QueryAabb(ref state, static (
                ref (List<FixtureProxy> pairBuffer, FixtureProxy proxy) tuple,
                in FixtureProxy other) =>
            {
                DebugTools.Assert(other.Fixture.Body.CanCollide);
                // Logger.DebugS("physics", $"Checking {proxy.Fixture.Body.Owner} against {other.Fixture.Body.Owner} at {aabb}");

                if (tuple.proxy == other ||
                    !ContactManager.ShouldCollide(tuple.proxy.Fixture, other.Fixture) ||
                    tuple.proxy.Fixture.Body == other.Fixture.Body)
                {
                    return true;
                }

                tuple.pairBuffer.Add(other);
                return true;
            }, aabb, true);
        }

        public void RegenerateContacts(PhysicsComponent body, TransformComponent? xform = null, FixturesComponent? fixtures = null)
        {
            _physicsSystem.DestroyContacts(body);
            if (!Resolve(body.Owner, ref xform, ref fixtures))
                return;

            if (!_lookup.TryGetCurrentBroadphase(xform, out var broadphase))
                return;

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                TouchProxies(xform.MapID, broadphase, fixture);
            }
        }

        private void TouchProxies(MapId mapId, BroadphaseComponent broadphase, Fixture fixture)
        {
            var broadphasePos = Transform(broadphase.Owner).WorldMatrix;

            foreach (var proxy in fixture.Proxies)
            {
                AddToMoveBuffer(mapId, proxy, broadphasePos.TransformBox(proxy.AABB));
            }
        }

        private void AddToMoveBuffer(MapId mapId, FixtureProxy proxy, Box2 aabb)
        {
            if (!TryComp<SharedPhysicsMapComponent>(_mapManager.GetMapEntityId(mapId), out var physicsMap))
                return;

            DebugTools.Assert(proxy.Fixture.Body.CanCollide);

            physicsMap.MoveBuffer[proxy] = aabb;
        }

        public void Refilter(Fixture fixture, TransformComponent? xform = null)
        {
            foreach (var contact in fixture.Contacts.Values)
            {
                contact.Flags |= ContactFlags.Filter;
            }

            if (!Resolve(fixture.Body.Owner, ref xform))
                return;

            if (!_lookup.TryGetCurrentBroadphase(xform, out var broadphase))
                return;

            TouchProxies(xform.MapID, broadphase, fixture);
        }

        // TODO: The below is slow and should just query the map's broadphase directly. The problem is that
        // there's some ordering stuff going on where the broadphase has queued all of its updates but hasn't applied
        // them yet so this query will fail on initialization which chains into a whole lot of issues.
        internal IEnumerable<BroadphaseComponent> GetBroadphases(MapId mapId, Box2 aabb)
        {
            // TODO Okay so problem: If we just do Encloses that's a lot faster BUT it also means we don't return the
            // map's broadphase which avoids us iterating over it for 99% of bodies.

            if (mapId == MapId.Nullspace) yield break;

            var enumerator = AllEntityQuery<BroadphaseComponent, TransformComponent>();

            while (enumerator.MoveNext(out var broadphase, out var xform))
            {
                if (xform.MapID != mapId) continue;

                if (!EntityManager.TryGetComponent(broadphase.Owner, out MapGridComponent? mapGrid))
                {
                    yield return broadphase;
                    continue;
                }

                // Won't worry about accurate bounds checks as it's probably slower in most use cases.
                var chunkEnumerator = mapGrid.GetMapChunks(aabb);

                if (chunkEnumerator.MoveNext(out _))
                {
                    yield return broadphase;
                }
            }
        }
    }
}
