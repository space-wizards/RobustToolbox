using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedBroadphaseSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IMapManagerInternal _mapManager = default!;
    [Dependency] private IParallelManager _parallel = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedGridTraversalSystem _traversal = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<BroadphaseComponent> _broadphaseQuery = default!;
    [Dependency] private EntityQuery<FixturesComponent> _fixturesQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    private readonly HashSet<FixtureProxy> _gridMoveBuffer = new();

    private float _frameTime;

    /*
     * Okay so Box2D has its own "MoveProxy" stuff so you can easily find new contacts when required.
     * Our problem is that we have nested broadphases (rather than being on separate maps) which makes this
     * not feasible because a body could be intersecting 2 broadphases.
     * Hence we need to check which broadphases it does intersect and checkar for colliding bodies.
     */

    private BroadphaseContactJob _contactJob;

    public override void Initialize()
    {
        base.Initialize();

        _contactJob = new()
        {
            MapManager = _mapManager,
            System = this,
            TransformSys = EntityManager.System<SharedTransformSystem>(),
            // TODO: EntityManager one isn't ready yet?
            XformQuery = GetEntityQuery<TransformComponent>(),
        };

        UpdatesOutsidePrediction = true;
        UpdatesAfter.Add(typeof(SharedTransformSystem));

        Subs.CVar(_cfg,
            CVars.TargetMinimumTickrate,
            val =>
            {
                _frameTime = 1f / val;
            },
            true);
    }

    public void Rebuild(BroadphaseComponent component, bool fullBuild)
    {
        component.StaticTree.Rebuild(fullBuild);
        component.DynamicTree.Rebuild(fullBuild);
        component.SundriesTree._b2Tree.Rebuild(fullBuild);
        component.StaticSundriesTree._b2Tree.Rebuild(fullBuild);
    }

    public void RebuildBottomUp(BroadphaseComponent component)
    {
        component.StaticTree.RebuildBottomUp();
        component.DynamicTree.RebuildBottomUp();
        component.SundriesTree._b2Tree.RebuildBottomUp();
        component.StaticSundriesTree._b2Tree.RebuildBottomUp();
    }

    #region Find Contacts

    /// <summary>
    /// Check the AABB for each moved broadphase fixture and add any colliding entities to the movebuffer in case.
    /// </summary>
    private void FindGridContacts(HashSet<EntityUid> movedGrids)
    {
        // None moved this tick
        if (movedGrids.Count == 0) return;

        // This is so that if we're on a broadphase that's moving (e.g. a grid) we need to make sure anything
        // we move over is getting checked for collisions, and putting it on the movebuffer is the easiest way to do so.
        var moveBuffer = _physicsSystem.MoveBuffer;
        _gridMoveBuffer.Clear();

        foreach (var gridUid in movedGrids)
        {
            var grid = _gridQuery.GetComponent(gridUid);
            var xform = _xformQuery.GetComponent(gridUid);

            // Moved to nullspace?
            if (!_broadphaseQuery.TryComp(xform.MapUid, out var mapBroadphase))
                continue;

            var worldAABB = _transform.GetWorldMatrix(xform).TransformBox(grid.LocalAABB);
            var enlargedAABB = worldAABB.Enlarged(GetBroadphaseExpand(_physicsQuery.GetComponent(gridUid), _frameTime));
            var state = (moveBuffer, _gridMoveBuffer);

            QueryMapBroadphase(mapBroadphase.DynamicTree, ref state, enlargedAABB);
            QueryMapBroadphase(mapBroadphase.StaticTree, ref state, enlargedAABB);
        }

        foreach (var proxy in _gridMoveBuffer)
        {
            moveBuffer.Add(proxy);
            // If something is in our AABB then try grid traversal for it
            _traversal.CheckTraverse((proxy.Entity, _xformQuery.GetComponent(proxy.Entity)));
        }
    }

    private float GetBroadphaseExpand(PhysicsComponent body, float frameTime)
    {
        return body.LinearVelocity.Length() * 1.2f * frameTime;
    }

    private void QueryMapBroadphase(IBroadPhase broadPhase,
        ref (HashSet<FixtureProxy>, HashSet<FixtureProxy>) state,
        Box2 enlargedAABB)
    {
        // Easier to just not go over each proxy as we already unioned the fixture's worldaabb.
        broadPhase.QueryAabb(ref state, static (ref (
                HashSet<FixtureProxy> moveBuffer,
                HashSet<FixtureProxy> gridMoveBuffer) tuple,
            in FixtureProxy value) =>
        {
            // 99% of the time it's just going to be the broadphase (for now the grid) itself.
            // hence this body check makes this run significantly better.
            // Also check if it's not already on the movebuffer.
            if (tuple.moveBuffer.Contains(value))
                return true;

            // To avoid updating during iteration.
            // Don't need to transform as it's already in map terms.
            tuple.gridMoveBuffer.Add(value);
            return true;
        }, enlargedAABB, true);
    }

    /// <summary>
    /// Go through every single created, moved, or touched proxy on the map and try to find any new contacts that should be created.
    /// </summary>
    internal void FindNewContacts()
    {
        _contactJob.FrameTime = _frameTime;
        _contactJob.Pairs.Clear();

        var moveBuffer = _physicsSystem.MoveBuffer;
        var movedGrids = _physicsSystem.MovedGrids;

        // Find any entities being driven over that might need to be considered
        FindGridContacts(movedGrids);

        // There is some mariana trench levels of bullshit going on.
        // We essentially need to re-create Box2D's FindNewContacts but in a way that allows us to check every
        // broadphase intersecting a particular proxy instead of just on the 1 broadphase.
        // This means we can generate contacts across different broadphases.
        // If you have a better way of allowing for broadphases attached to grids then by all means code it yourself.

        // FindNewContacts is inherently going to be a lot slower than Box2D's normal version so we need
        // to cache a bunch of stuff to make up for it.

        // Handle grids first as they're not stored on map broadphase at all.
        HandleGridCollisions(movedGrids);

        // EZ
        if (moveBuffer.Count == 0)
            return;

        _contactJob.MoveBuffer.Clear();

        foreach (var proxy in moveBuffer)
        {
            DebugTools.Assert(_xformQuery.GetComponent(proxy.Entity).Broadphase?.Uid != null);
            _contactJob.MoveBuffer.Add(proxy);
        }

        var count = moveBuffer.Count;

        _parallel.ProcessNow(_contactJob, count);

        foreach (var (proxyA, proxyB, flags) in _contactJob.Pairs)
        {
            var otherBody = proxyB.Body;
            var contactFlags = ContactFlags.None;

            // Because we may be colliding with something asleep (due to the way grid movement works) need
            // to make sure the contact doesn't fail.
            // This is because we generate a contact across 2 different broadphases where both bodies aren't
            // moving locally but are moving in world-terms.
            if ((flags & PairFlag.Wake) == PairFlag.Wake)
            {
                _physicsSystem.WakeBody(proxyA.Entity, force: true, body: proxyA.Body);
                _physicsSystem.WakeBody(proxyB.Entity, force: true, body: otherBody);
            }

            // TODO: Actually implement this for grids, atm they have their own skrungly fixture handling which prevents this.
            if ((PairFlag.Grid & flags) == PairFlag.Grid)
            {
                contactFlags |= ContactFlags.Grid;
            }

            _physicsSystem.AddPair(proxyA.FixtureId, proxyB.FixtureId, proxyA, proxyB, flags: contactFlags);
        }

        moveBuffer.Clear();
        movedGrids.Clear();
    }

    private void HandleGridCollisions(HashSet<EntityUid> movedGrids)
    {
        // TODO: Could move this into its own job.
        // Ideally we'd just have some way to flag an entity as "AABB moves not proxy" into its own movebuffer.
        foreach (var gridAUid in movedGrids)
        {
            var gridAMapComp = _gridQuery.GetComponent(gridAUid);
            var xform = _xformQuery.GetComponent(gridAUid);

            if (xform.MapID == MapId.Nullspace)
                continue;

            var (worldPos, worldRot, gridAToWorld, worldToGridA) = _transform.GetWorldPositionRotationMatrixWithInv(xform);

            var aabb = new Box2Rotated(gridAMapComp.LocalAABB, worldRot).CalcBoundingBox().Translated(worldPos);

            // TODO: Need to handle grids colliding with non-grid entities with the same layer
            // (nothing in SS14 does this yet).
            var fixture = _fixturesQuery.Comp(gridAUid);
            var physics = _physicsQuery.Comp(gridAUid);

            // This is just a rigid transform, exactly the same as gridAToWorld //TODO: Really shouldn't need this!
            var gridAToWorldRigid = _physicsSystem.GetPhysicsTransform(gridAUid);
            var state = (
                new Entity<FixturesComponent, MapGridComponent, PhysicsComponent, TransformComponent>(gridAUid, fixture, gridAMapComp, physics, xform),
                gridAToWorldRigid,
                gridAToWorld,
                worldToGridA,
                _map,
                _physicsSystem,
                _transform,
                _fixturesQuery,
                _physicsQuery,
                _xformQuery);

            _mapManager.FindGridsIntersecting(xform.MapID, aabb, ref state,
                static (EntityUid gridBUid, MapGridComponent gridBMapComp,
                    ref (Entity<FixturesComponent, MapGridComponent, PhysicsComponent, TransformComponent> gridA,
                        Transform gridAToWorldRigid,
                        Matrix3x2 gridAToWorld,
                        Matrix3x2 worldToGridA,
                        SharedMapSystem _map,
                        SharedPhysicsSystem _physicsSystem,
                        SharedTransformSystem xformSystem,
                        EntityQuery<FixturesComponent> fixturesQuery,
                        EntityQuery<PhysicsComponent> physicsQuery,
                        EntityQuery<TransformComponent> xformQuery) tuple) =>
                {
                    if (tuple.gridA.Owner == gridBUid ||
                        !tuple.xformQuery.TryGetComponent(gridBUid, out var collidingXform))
                    {
                        return true;
                    }

                    // If the other entity is lower ID and also moved then let that handle the collision.
                    if (tuple.gridA.Owner.Id > gridBUid.Id && tuple._physicsSystem.MovedGrids.Contains(gridBUid))
                    {
                        return true;
                    }

                    var (_, _, gridBToWorld, worldToGridB) =  tuple.xformSystem.GetWorldPositionRotationMatrixWithInv(collidingXform);
                    var gridBAabbInWorld = gridBToWorld.TransformBox(gridBMapComp.LocalAABB);
                    // Same as gridBToWorld, just a rigid transform:
                    var gridBToWorldRigid = tuple._physicsSystem.GetPhysicsTransform(gridBUid);

                    // Get Grid2 AABB in grid1 ref
                    var intersectionAabbInGridA = tuple.gridA.Comp2.LocalAABB.Intersect(tuple.worldToGridA.TransformBox(gridBAabbInWorld));

                    // TODO: AddPair has a nasty check in there that's O(n) but that's also a general physics problem.
                    var gridAChunks = tuple._map.GetLocalMapChunks(tuple.gridA.Owner, tuple.gridA, intersectionAabbInGridA);
                    var physicsA = tuple.gridA.Comp3;
                    var physicsB = tuple.physicsQuery.GetComponent(gridBUid);
                    var fixturesB = tuple.fixturesQuery.Comp(gridBUid);

                    // Only care about chunks on other grid overlapping us.
                    while (gridAChunks.MoveNext(out var curChunkA))
                    {
                        var chunkAAabbInWorld =
                            tuple.gridAToWorld.TransformBox(
                                curChunkA.CachedBounds.Translated(curChunkA.Indices * tuple.gridA.Comp2.ChunkSize));
                        var chunkAAabbInGridB = worldToGridB.TransformBox(chunkAAabbInWorld);
                        var gridBOverlaps = tuple._map.GetLocalMapChunks(gridBUid, gridBMapComp, chunkAAabbInGridB);

                        while (gridBOverlaps.MoveNext(out var curChunkB))
                        {
                            foreach (var fixAId in curChunkA.Fixtures)
                            {
                                var fixtureA = tuple.gridA.Comp1.Fixtures[fixAId];

                                foreach (var fixBId in curChunkB.Fixtures)
                                {
                                    var fixtureB = fixturesB.Fixtures[fixBId];

                                    // There's already a contact so ignore it.
                                    if (fixtureA.Contacts.ContainsKey(fixtureB))
                                        continue;

                                    bool addedPair = false;
                                    for (var i = 0; i < fixtureA.Shape.ChildCount && !addedPair; i++)
                                    {
                                        var shapeAAabbInWorld = fixtureA.Shape.ComputeAABB(tuple.gridAToWorldRigid, i);

                                        for (var j = 0; j < fixtureB.Shape.ChildCount && !addedPair; j++)
                                        {
                                            var shapeBAabbInWorld = fixtureB.Shape.ComputeAABB(gridBToWorldRigid, j);

                                            if (!shapeAAabbInWorld.Intersects(shapeBAabbInWorld)) continue;

                                            tuple._physicsSystem.AddPair(
                                                (tuple.gridA.Owner, tuple.gridA.Comp3, tuple.gridA.Comp4),
                                                (gridBUid, physicsB, collidingXform),
                                                fixAId, fixBId,
                                                fixtureA, i,
                                                fixtureB, j,
                                                physicsA, physicsB,
                                                ContactFlags.Grid);

                                            // Early out now and ignore every other i/j pair
                                            addedPair = true;
                                            // TODO: AddPair only handles a *single* contact between multiple
                                            // fixtures, even if the contact is added for distinct child shapes i
                                            // and j. Right now, there is only one child shape per fixture, but
                                            // this code will fail to detect some collisions if this assumption is
                                            // ever changed!
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
        List<(FixtureProxy, FixtureProxy, PairFlag)> pairBuffer)
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
        var state = (pairBuffer, _physicsSystem.MoveBuffer, this, _physicsSystem, proxy);

        QueryBroadphase(broadphaseComp.DynamicTree, state, aabb);

        if ((proxy.Body.BodyType & BodyType.Static) != 0x0)
            return;

        QueryBroadphase(broadphaseComp.StaticTree, state, aabb);
    }

    private void QueryBroadphase(IBroadPhase broadPhase, (List<(FixtureProxy, FixtureProxy, PairFlag)>, HashSet<FixtureProxy> MoveBuffer, SharedBroadphaseSystem Broadphase, SharedPhysicsSystem PhysicsSystem, FixtureProxy) state, Box2 aabb)
    {
        broadPhase.QueryAabb(ref state, static (
            ref (List<(FixtureProxy, FixtureProxy, PairFlag)> pairs, HashSet<FixtureProxy> moveBuffer, SharedBroadphaseSystem broadphase, SharedPhysicsSystem physicsSystem, FixtureProxy proxy) tuple,
            in FixtureProxy other) =>
        {
            DebugTools.Assert(other.Body.CanCollide);
            // Logger.DebugS("physics", $"Checking {proxy.Entity} against {other.Fixture.Body.Owner} at {aabb}");

            if (tuple.proxy.Entity == other.Entity ||
                !SharedPhysicsSystem.ShouldCollide(tuple.proxy.Fixture, other.Fixture))
                return true;

            // Avoid creating duplicate pairs.
            // We give priority to whoever has the lower entity ID.
            if (tuple.proxy.Entity.Id > other.Entity.Id)
            {
                // Let the other fixture handle it.
                if (tuple.moveBuffer.Contains(other))
                    return true;
            }

            // Check if contact already exists.
            if (tuple.proxy.Fixture.Contacts.ContainsKey(other.Fixture))
                return true;

            // TODO: Add in the slow path check here but turnstiles currently explodes this on content so.
            if (!tuple.physicsSystem.ShouldCollideJoints(tuple.proxy.Entity, other.Entity))
                return true;

            // TODO: Sensors handled elsewhere when we do v3 port.
            //if (!tuple.proxy.Fixture.Hard || !other.Fixture.Hard)
            //    return true;

            // TODO: Check if interlocked + array is better here which is what box2d does
            // It then just heap allocates anything over the array size.
            var flags = PairFlag.None;
            if (tuple.proxy.Fixture.Hard &&
                other.Fixture.Hard &&
                (tuple.broadphase._gridMoveBuffer.Contains(tuple.proxy) || tuple.broadphase._gridMoveBuffer.Contains(other)))
                flags |= PairFlag.Wake;

            lock (tuple.pairs)
            {
                tuple.pairs.Add((tuple.proxy, other, flags));
            }

            return true;
        }, aabb, true);
    }

    [Obsolete("Use Entity<T> variant")]
    public void RegenerateContacts(EntityUid uid, PhysicsComponent body, FixturesComponent? fixtures = null, TransformComponent? xform = null)
    {
        RegenerateContacts((uid, body, fixtures, xform));
    }

    public void RegenerateContacts(Entity<PhysicsComponent?, FixturesComponent?, TransformComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp1))
            return;

        _physicsSystem.DestroyContacts(entity.Comp1);

        if (!Resolve(entity.Owner, ref entity.Comp2 , ref entity.Comp3))
            return;

        if (entity.Comp3.MapUid == null)
            return;

        if (!_xformQuery.TryGetComponent(entity.Comp3.Broadphase?.Uid, out var broadphase))
            return;

        _physicsSystem.SetAwake(entity!, true);

        foreach (var fixture in entity.Comp2.Fixtures.Values)
        {
            TouchProxies(fixture);
        }
    }

    internal void TouchProxies(Fixture fixture)
    {
        foreach (var proxy in fixture.Proxies)
        {
            AddToMoveBuffer(proxy);
        }
    }

    private void AddToMoveBuffer(FixtureProxy proxy)
    {
        DebugTools.Assert(proxy.Body.CanCollide);
        _physicsSystem.MoveBuffer.Add(proxy);
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

        TouchProxies(fixture);
    }

    internal void GetBroadphases(MapId mapId, Box2 aabb, BroadphaseCallback callback)
    {
        var internalState = (callback, _broadphaseQuery);

        if (!_map.TryGetMap(mapId, out var map))
            return;

        if (_broadphaseQuery.TryGetComponent(map.Value, out var mapBroadphase))
            callback((map.Value, mapBroadphase));

        _mapManager.FindGridsIntersecting(map.Value,
            aabb,
            ref internalState,
            static (
                EntityUid uid,
                MapGridComponent _,
                ref (BroadphaseCallback callback, EntityQuery<BroadphaseComponent> _broadphaseQuery) tuple) =>
            {
                if (tuple._broadphaseQuery.TryComp(uid, out var broadphase))
                    tuple.callback((uid, broadphase));

                return true;
            },
            // Approx because we don't really need accurate checks for these most of the time.
            approx: true,
            includeMap: false);
    }

    internal void GetBroadphases<TState>(MapId mapId, Box2 aabb, ref TState state, BroadphaseCallback<TState> callback)
    {
        var internalState = (state, callback, _broadphaseQuery);

        if (!_map.TryGetMap(mapId, out var map))
            return;

        if (_broadphaseQuery.TryGetComponent(map.Value, out var mapBroadphase))
            callback((map.Value, mapBroadphase), ref state);

        _mapManager.FindGridsIntersecting(map.Value,
            aabb,
            ref internalState,
            static (
                EntityUid uid,
                MapGridComponent _,
                ref (TState state, BroadphaseCallback<TState> callback, EntityQuery<BroadphaseComponent> _broadphaseQuery) tuple) =>
            {
                if (tuple._broadphaseQuery.TryComp(uid, out var broadphase))
                    tuple.callback((uid, broadphase), ref tuple.state);
                return true;
            },
            // Approx because we don't really need accurate checks for these most of the time.
            approx: true,
            includeMap: false);

        state = internalState.state;
    }

    internal delegate void BroadphaseCallback(Entity<BroadphaseComponent> entity);

    internal delegate void BroadphaseCallback<TState>(Entity<BroadphaseComponent> entity, ref TState state);

    private record struct BroadphaseContactJob() : IParallelRobustJob
    {
        public SharedBroadphaseSystem System = default!;
        public SharedTransformSystem TransformSys = default!;
        public IMapManager MapManager = default!;

        public EntityQuery<TransformComponent> XformQuery;

        public readonly List<FixtureProxy> MoveBuffer = new();

        public List<(FixtureProxy, FixtureProxy, PairFlag)> Pairs = new(64);

        public float FrameTime;

        // Box2D uses 64 but we have to do grid queries for each fixtureproxy which will add a fair bit of overhead.
        // Plus we also run events + trycomp for joints on top.
        public int BatchSize => 16;

        public void Execute(int index)
        {
            var proxy = MoveBuffer[index];
            var broadphaseUid = XformQuery.GetComponent(proxy.Entity).Broadphase?.Uid;
            var worldAABB = TransformSys.GetWorldMatrix(broadphaseUid!.Value).TransformBox(proxy.AABB);

            var mapUid = XformQuery.GetComponent(proxy.Entity).MapUid ?? EntityUid.Invalid;

            var broadphaseExpand = System.GetBroadphaseExpand(proxy.Body, FrameTime);

            var proxyBody = proxy.Body;
            DebugTools.Assert(!proxyBody.Deleted);

            var state = (System, proxy, worldAABB, Pairs);

            // Get every broadphase we may be intersecting.
            MapManager.FindGridsIntersecting(mapUid, worldAABB.Enlarged(broadphaseExpand), ref state,
                static (EntityUid uid, MapGridComponent _, ref (
                    SharedBroadphaseSystem system,
                    FixtureProxy proxy,
                    Box2 worldAABB,
                    List<(FixtureProxy, FixtureProxy, PairFlag)> pairBuffer) tuple) =>
                {
                    ref var buffer = ref tuple.pairBuffer;
                    tuple.system.FindPairs(tuple.proxy, tuple.worldAABB, uid, buffer);
                    return true;
                },
                approx: true,
                includeMap: false);

            // Struct ref moment, I have no idea what's fastest.
            System.FindPairs(proxy, worldAABB, mapUid, Pairs);
        }
    }

    [Flags]
    private enum PairFlag : byte
    {
        None = 0,

        /// <summary>
        /// Should we wake the contacting entities.
        /// </summary>
        Wake = 1 << 0,

        /// <summary>
        /// Is it a grid collision.
        /// </summary>
        Grid = 1 << 1,
    }
}
