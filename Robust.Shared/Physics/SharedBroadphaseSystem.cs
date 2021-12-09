using System;
using System.Collections.Generic;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    public abstract class SharedBroadphaseSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        private const int MinimumBroadphaseCapacity = 256;

        // We queue updates rather than handle them immediately for multiple reasons
        // A) Entity initializing may call several events which only need handling once so we'd need to add a bunch of code to account for what stage of initializing they're at
        // B) It's faster for instances like MoveEvent and RotateEvent both being issued

        /*
         * Okay so Box2D has its own "MoveProxy" stuff so you can easily find new contacts when required.
         * Our problem is that we have nested broadphases (rather than being on separate maps) which makes this
         * not feasible because a body could be intersecting 2 broadphases.
         * Hence we need to check which broadphases it does intersect and checkar for colliding bodies.
         */

        // We keep 2 move buffers as we need to handle the broadphase moving behavior first.
        // This is because we'll chuck anything the broadphase moves over onto the movebuffer so contacts can be generated.
        private Dictionary<MapId, Dictionary<FixtureProxy, Box2>> _moveBuffer = new();

        // Cache moved grids so we can just check our overall bounds and not each proxy for FindGridContacts
        private Dictionary<MapId, HashSet<GridId>> _movedGrids = new();

        // Caching for FindNewContacts
        private Dictionary<FixtureProxy, HashSet<FixtureProxy>> _pairBuffer = new(64);
        private Dictionary<EntityUid, Box2> _broadphaseBounding = new(8);
        private Dictionary<EntityUid, Matrix3> _broadphaseInvMatrices = new(8);
        private HashSet<EntityUid> _broadphases = new(8);
        private Dictionary<FixtureProxy, Box2> _gridMoveBuffer = new(64);
        private List<FixtureProxy> _queryBuffer = new(32);

        // Caching for Synchronize
        private Dictionary<BroadphaseComponent, (Vector2 Position, float Rotation)> _broadphaseTransforms = new();

        /// <summary>
        /// How much to expand bounds by to check cross-broadphase collisions.
        /// Ideally you want to set this to your largest body size.
        /// This only has a noticeable performance impact where multiple broadphases are in close proximity.
        /// </summary>
        private float _broadphaseExpand;

        public override void Initialize()
        {
            base.Initialize();
            UpdatesAfter.Add(typeof(SharedTransformSystem));

            SubscribeLocalEvent<BroadphaseComponent, ComponentInit>(HandleBroadphaseInit);
            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInit);

            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerInsert);
            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleContainerRemove);
            SubscribeLocalEvent<PhysicsUpdateMessage>(OnPhysicsUpdate);

            // Shouldn't need to listen to mapchanges as parent changes should handle it...
            SubscribeLocalEvent<PhysicsComponent, EntParentChangedMessage>(OnParentChange);

            SubscribeLocalEvent<PhysicsComponent, MoveEvent>(HandleMove);
            SubscribeLocalEvent<PhysicsComponent, RotateEvent>(HandleRotate);

            SubscribeLocalEvent<MapGridComponent, MoveEvent>(OnGridMove);
            SubscribeLocalEvent<MapGridComponent, EntMapIdChangedMessage>(OnGridMapChange);

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.BroadphaseExpand, SetBroadphaseExpand, true);
            _mapManager.MapCreated += OnMapCreated;
            _mapManager.MapDestroyed += OnMapDestroyed;
        }

        private void SetBroadphaseExpand(float value) => _broadphaseExpand = value;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            ProcessUpdates();
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            ProcessUpdates();
        }

        /// <summary>
        /// Go through every deferred event and update the broadphase.
        /// </summary>
        public void ProcessUpdates()
        {
            EnsureBroadphaseTransforms();

            _broadphaseBounding.Clear();
            _broadphases.Clear();
            _broadphaseTransforms.Clear();
            // Unfortunately we can't re-use our broadphase transforms as controllers may update them.
            _physicsManager.ClearTransforms();
        }

        // because physics is damn expensive we're gonna cache as much broadphase data up front as we can to re-use across
        // all bodies that need updating.
        internal void EnsureBroadphaseTransforms()
        {
            // Cache as much broadphase data as we can up front for this map.
            foreach (var broadphase in EntityManager.EntityQuery<BroadphaseComponent>(true))
            {
                UpdateBroadphaseCache(broadphase);
            }
        }

        internal void UpdateBroadphaseCache(BroadphaseComponent broadphase)
        {
            var uid = broadphase.OwnerUid;

            var xformComp = EntityManager.GetComponent<TransformComponent>(uid);

            var matrix = xformComp.WorldMatrix;
            var worldPosition = new Vector2(matrix.R0C2, matrix.R1C2);
            var transform = new Transform(worldPosition, xformComp.WorldRotation);

            _physicsManager.SetTransform(uid, transform);
            _broadphases.Add(uid);
            _broadphaseTransforms[broadphase] = (transform.Position, transform.Quaternion2D.Angle);
            _broadphaseInvMatrices[uid] = xformComp.InvWorldMatrix;

            if (EntityManager.TryGetComponent(uid, out IMapGridComponent? mapGrid))
            {
                _broadphaseBounding[uid] = matrix.TransformBox(mapGrid.Grid.LocalBounds);
            }
            else
            {
                DebugTools.Assert(!EntityManager.HasComponent<PhysicsComponent>(uid));
            }
        }

        #region Find Contacts

        /// <summary>
        /// Check the AABB for each moved broadphase fixture and add any colliding entities to the movebuffer in case.
        /// </summary>
        private void FindGridContacts(MapId mapId)
        {
            // None moved this tick
            if (!_movedGrids.TryGetValue(mapId, out var movedGrids)) return;

            var mapBroadphase = EntityManager.GetComponent<BroadphaseComponent>(_mapManager.GetMapEntityId(mapId));

            // This is so that if we're on a broadphase that's moving (e.g. a grid) we need to make sure anything
            // we move over is getting checked for collisions, and putting it on the movebuffer is the easiest way to do so.
            var moveBuffer = _moveBuffer[mapId];

            foreach (var movedGrid in movedGrids)
            {
                if (!_mapManager.TryGetGrid(movedGrid, out var grid))
                    continue;

                DebugTools.Assert(grid.ParentMapId == mapId);
                var worldAABB = grid.WorldBounds;
                var enlargedAABB = worldAABB.Enlarged(_broadphaseExpand);

                var gridBody = EntityManager.GetComponent<PhysicsComponent>(grid.GridEntityId);

                // TODO: Use the callback for this you ape.
                // Easier to just not go over each proxy as we already unioned the fixture's worldaabb.
                foreach (var other in mapBroadphase.Tree.QueryAabb(_queryBuffer, enlargedAABB))
                {
                    // 99% of the time it's just going to be the broadphase (for now the grid) itself.
                    // hence this body check makes this run significantly better.
                    // Also check if it's not already on the movebuffer.
                    if (other.Fixture.Body == gridBody || moveBuffer.ContainsKey(other)) continue;

                    // To avoid updating during iteration.
                    // Don't need to transform as it's already in map terms.
                    _gridMoveBuffer[other] = other.AABB;
                }

                _queryBuffer.Clear();
            }

            foreach (var (proxy, worldAABB) in _gridMoveBuffer)
            {
                moveBuffer[proxy] = worldAABB;
            }

            movedGrids.Clear();
        }

        /// <summary>
        /// Go through every single created, moved, or touched proxy on the map and try to find any new contacts that should be created.
        /// </summary>
        internal void FindNewContacts(MapId mapId)
        {
            var moveBuffer = _moveBuffer[mapId];

            if (moveBuffer.Count == 0) return;

            // Find any entities being driven over that might need to be considered
            FindGridContacts(mapId);

            // There is some mariana trench levels of bullshit going on.
            // We essentially need to re-create Box2D's FindNewContacts but in a way that allows us to check every
            // broadphase intersecting a particular proxy instead of just on the 1 broadphase.
            // This means we can generate contacts across different broadphases.
            // If you have a better way of allowing for broadphases attached to grids then by all means code it yourself.

            // FindNewContacts is inherently going to be a lot slower than Box2D's normal version so we need
            // to cache a bunch of stuff to make up for it.
            var contactManager = _mapManager.GetMapEntity(mapId).GetComponent<SharedPhysicsMapComponent>().ContactManager;

            // TODO: Could store fixtures by broadphase for more perf?
            foreach (var (proxy, worldAABB) in moveBuffer)
            {
                var proxyBody = proxy.Fixture.Body;
                // if (prediction && !proxyBody.Predict) continue;

                // Get every broadphase we may be intersecting.
                foreach (var (broadphase, _) in _broadphaseTransforms)
                {
                    // Broadphase can't intersect with entities on itself so skip.
                    if (proxyBody.OwnerUid == broadphase.OwnerUid ||
                        broadphase.Owner.Transform.MapID != proxyBody.Owner.Transform.MapID) continue;

                    var enlargedAABB = worldAABB.Enlarged(_broadphaseExpand);

                    // If we're a map / our BB intersects then we'll do the work
                    if (_broadphaseBounding.TryGetValue(broadphase.OwnerUid, out var broadphaseAABB) &&
                        !broadphaseAABB.Intersects(enlargedAABB)) continue;

                    // Logger.DebugS("physics", $"Checking proxy for {proxy.Fixture.Body.Owner} on {broadphase.Owner}");
                    Box2 aabb;
                    var proxyBroad = proxyBody.Broadphase!;

                    // If it's the same broadphase as our body's one then don't need to translate the AABB.
                    if (proxyBroad == broadphase)
                    {
                        aabb = proxy.AABB;
                    }
                    else
                    {
                        aabb = _broadphaseInvMatrices[broadphase.OwnerUid].TransformBox(worldAABB);
                    }

                    foreach (var other in broadphase.Tree.QueryAabb(_queryBuffer, aabb))
                    {
                        // Logger.DebugS("physics", $"Checking {proxy.Fixture.Body.Owner} against {other.Fixture.Body.Owner} at {aabb}");

                        // Do fast checks first and slower checks after (in ContactManager).
                        if (proxy == other ||
                            proxy.Fixture.Body == other.Fixture.Body ||
                            !ContactManager.ShouldCollide(proxy.Fixture, other.Fixture)) continue;

                        // Don't add duplicates.
                        // Look it disgusts me but we can't do it Box2D's way because we're getting pairs
                        // with different broadphases so can't use Proxy sorting to skip duplicates.
                        // TODO: This needs to be better
                        if (_pairBuffer.TryGetValue(other, out var existing) &&
                            existing.Contains(proxy))
                        {
                            continue;
                        }

                        if (!_pairBuffer.TryGetValue(proxy, out var proxyExisting))
                        {
                            proxyExisting = new HashSet<FixtureProxy>();
                            _pairBuffer[proxy] = proxyExisting;
                        }

                        proxyExisting.Add(other);
                    }

                    _queryBuffer.Clear();
                }
            }

            foreach (var (proxyA, proxies) in _pairBuffer)
            {
                if (proxyA.Fixture.Body.Deleted) continue;

                foreach (var other in proxies)
                {
                    if (other.Fixture.Body.Deleted) continue;

                    // Because we may be colliding with something asleep (due to the way grid movement works) need
                    // to make sure the contact doesn't fail.
                    // This is because we generate a contact across 2 different broadphases where both bodies aren't
                    // moving locally but are moving in world-terms.
                    if (proxyA.Fixture.Hard && other.Fixture.Hard)
                    {
                        proxyA.Fixture.Body.WakeBody();
                        other.Fixture.Body.WakeBody();
                    }

                    contactManager.AddPair(proxyA, other);
                }
            }

            _pairBuffer.Clear();
            _moveBuffer[mapId].Clear();
            _gridMoveBuffer.Clear();
        }

        #endregion

        internal void Cleanup()
        {
            // Can't just clear movebuffer / movedgrids here because this is called after transforms update.
            _broadphaseBounding.Clear();
            _broadphaseTransforms.Clear();
            _broadphaseInvMatrices.Clear();
        }

        private void OnParentChange(EntityUid uid, PhysicsComponent component, ref EntParentChangedMessage args)
        {
            if (!component.CanCollide || EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage <
                EntityLifeStage.Initialized) return;

            UpdateBroadphase(component);
        }

        /// <summary>
        /// If our broadphase has changed then remove us from our old one and add to our new one.
        /// </summary>
        /// <param name="body"></param>
        private void UpdateBroadphase(PhysicsComponent body, FixturesComponent? manager = null)
        {
            if (!Resolve(body.OwnerUid, ref manager)) return;

            var oldBroadphase = body.Broadphase;
            var newBroadphase = GetBroadphase(body);

            if (oldBroadphase == newBroadphase) return;

            DestroyProxies(body, manager);

            // Shouldn't need to null-check as this already checks for nullspace so should be okay...?
            CreateProxies(body, manager);
        }

        /// <summary>
        /// Remove all of our fixtures from the broadphase.
        /// </summary>
        private void DestroyProxies(PhysicsComponent body, FixturesComponent? manager = null)
        {
            if (!Resolve(body.OwnerUid, ref manager)) return;

            var broadphase = body.Broadphase;

            if (broadphase == null) return;

            foreach (var (_, fixture) in manager.Fixtures)
            {
                DestroyProxies(broadphase, fixture);
            }

            body.Broadphase = null;
        }

        private void OnPhysicsUpdate(PhysicsUpdateMessage ev)
        {
            var lifestage = EntityManager.GetComponent<MetaDataComponent>(ev.Component.OwnerUid).EntityLifeStage;

            // Oh god kill it with fire.
            if (lifestage is < EntityLifeStage.Initialized or > EntityLifeStage.MapInitialized) return;

            if (ev.Component.CanCollide)
            {
                AddBody(ev.Component);
            }
            else
            {
                RemoveBody(ev.Component);
            }
        }

        public void AddBody(PhysicsComponent body, FixturesComponent? manager = null)
        {
            // TODO: Good idea? Ehhhhhhhhhhhh
            // The problem is there's some fuckery with events while an entity is initializing.
            // Can probably just bypass this by doing stuff in Update / FrameUpdate again but future problem
            //
            if (body.Broadphase != null) return;

            if (!Resolve(body.OwnerUid, ref manager))
            {
                return;
            }

            CreateProxies(body, manager);
        }

        internal void RemoveBody(PhysicsComponent body, FixturesComponent? manager = null)
        {
            // Not on any broadphase anyway.
            if (body.Broadphase == null) return;

            // TODO: Would reaaalllyy like for this to not be false in future
            if (!Resolve(body.OwnerUid, ref manager, false))
            {
                return;
            }

            DestroyProxies(body, manager);
        }

        private void OnGridMove(EntityUid uid, MapGridComponent component, ref MoveEvent args)
        {
            var mapId = EntityManager.GetComponent<TransformComponent>(uid).MapID;

            if (!_movedGrids.TryGetValue(mapId, out var gridMap))
            {
                gridMap = new HashSet<GridId>();
                _movedGrids[mapId] = gridMap;
            }

            gridMap.Add(component.GridIndex);
        }

        private void OnGridMapChange(EntityUid uid, MapGridComponent component, EntMapIdChangedMessage args)
        {
            // Make sure we cleanup old map for moved grid stuff.
            var mapId = EntityManager.GetComponent<TransformComponent>(uid).MapID;

            if (!_movedGrids.TryGetValue(mapId, out var movedGrids)) return;

            movedGrids.Remove(component.GridIndex);
        }

        public void RegenerateContacts(PhysicsComponent body)
        {
            var edge = body.ContactEdges;

            // TODO: PhysicsMap actually needs to be made nullable (or needs a re-design to not be on the body).
            // Eventually it'll be a component on the map so nullspace won't have one anyway and we need to handle that scenario.
            // Technically it is nullable coz of networking (previously it got away with being able to ignore it
            // but anchoring can touch BodyType in HandleComponentState so we need to handle this here).
            if (body.PhysicsMap != null)
            {
                var contactManager = body.PhysicsMap.ContactManager;

                while (edge != null)
                {
                    var ce0 = edge;
                    edge = edge.Next;
                    contactManager.Destroy(ce0.Contact!);
                }
            }
            else
            {
                DebugTools.Assert(body.ContactEdges == null);
            }

            body.ContactEdges = null;

            var broadphase = body.Broadphase;

            if (broadphase != null)
            {
                var mapId = body.Owner.Transform.MapID;

                foreach (var fixture in body.Fixtures)
                {
                    TouchProxies(mapId, broadphase, fixture);
                }
            }
        }

        public void Refilter(Fixture fixture)
        {
            // TODO: Call this method whenever collisionmask / collisionlayer changes
            if (fixture.Body == null) return;

            var body = fixture.Body;

            var edge = body.ContactEdges;

            while (edge != null)
            {
                var contact = edge.Contact!;
                var fixtureA = contact.FixtureA;
                var fixtureB = contact.FixtureB;

                if (fixtureA == fixture || fixtureB == fixture)
                {
                    contact.FilterFlag = true;
                }

                edge = edge.Next;
            }

            var broadphase = body.Broadphase;

            // If nullspace or whatever ignore it.
            if (broadphase == null) return;

            TouchProxies(fixture.Body.Owner.Transform.MapID, broadphase, fixture);
        }

        private void TouchProxies(MapId mapId, BroadphaseComponent broadphase, Fixture fixture)
        {
            var broadphasePos = broadphase.Owner.Transform.WorldPosition;

            foreach (var proxy in fixture.Proxies)
            {
                AddToMoveBuffer(mapId, proxy, proxy.AABB.Translated(broadphasePos));
            }
        }

        private void HandleMove(EntityUid uid, PhysicsComponent component, ref MoveEvent args)
        {
            if (!component.CanCollide) return;

            var worldRot = EntityManager.GetComponent<TransformComponent>(uid).WorldRotation;

            SynchronizeFixtures(component, args.NewPosition.ToMapPos(EntityManager), (float) worldRot.Theta);
        }

        private void HandleRotate(EntityUid uid, PhysicsComponent component, ref RotateEvent args)
        {
            if (!component.CanCollide) return;

            var worldPos = EntityManager.GetComponent<TransformComponent>(uid).WorldPosition;

            SynchronizeFixtures(component, worldPos, (float) args.NewRotation.Theta);
        }

        private void SynchronizeFixtures(PhysicsComponent body, Vector2 worldPos, float worldRot)
        {
            // Logger.DebugS("physics", $"Synchronizing fixtures for {body.Owner}");
            // Don't cache this as controllers may change it freely before we run physics!
            var xf = new Transform(worldPos, worldRot);

            if (body.Awake)
            {
                // TODO: SWEPT HERE
                // Check if we need to use the normal synchronize which also supports TOI
                // Otherwise, use the slightly faster one.

                // For now we'll just use the normal one as no TOI support
                foreach (var fixture in body.Fixtures)
                {
                    if (fixture.ProxyCount == 0) continue;

                    // SynchronizezTOI(fixture, xf1, xf2);

                    Synchronize(fixture, xf);
                }
            }
            else
            {
                foreach (var fixture in body.Fixtures)
                {
                    if (fixture.ProxyCount == 0) continue;

                    Synchronize(fixture, xf);
                }
            }

            // Ensure cache remains up to date if the broadphase is moving.
            var uid = body.OwnerUid;

            if (EntityManager.TryGetComponent(uid, out BroadphaseComponent? broadphase))
            {
                UpdateBroadphaseCache(broadphase);
            }
        }

        /// <summary>
        /// A more efficient Synchronize for 1 transform.
        /// </summary>
        private void Synchronize(Fixture fixture, Transform transform1)
        {
            // tl;dr update our bounding boxes stored in broadphase.
            var broadphase = fixture.Body.Broadphase!;
            var proxyCount = fixture.ProxyCount;

            var broadphaseXform = EntityManager.GetComponent<TransformComponent>(broadphase.OwnerUid);

            var broadphaseMapId = broadphaseXform.MapID;
            var (broadphaseWorldPos, broadphaseWorldRot, broadphaseInvMatrix) = broadphaseXform.GetWorldPositionRotationInvMatrix();

            var relativePos1 = new Transform(
                broadphaseInvMatrix.Transform(transform1.Position),
                transform1.Quaternion2D.Angle - broadphaseWorldRot);

            for (var i = 0; i < proxyCount; i++)
            {
                var proxy = fixture.Proxies[i];
                var bounds = fixture.Shape.ComputeAABB(relativePos1, i);
                proxy.AABB = bounds;
                var displacement = Vector2.Zero;
                broadphase.Tree.MoveProxy(proxy.ProxyId, bounds, displacement);

                var worldAABB = new Box2Rotated(bounds, broadphaseWorldRot, Vector2.Zero)
                    .CalcBoundingBox()
                    .Translated(broadphaseWorldPos);

                AddToMoveBuffer(broadphaseMapId, proxy, worldAABB);
            }
        }

        private void AddToMoveBuffer(MapId mapId, FixtureProxy proxy, Box2 aabb)
        {
            if(mapId == MapId.Nullspace)
                return;

            _moveBuffer[mapId][proxy] = aabb;
        }

        /// <summary>
        /// Get broadphase proxies from the body's fixtures and add them to the relevant broadphase.
        /// </summary>
        /// <param name="useCache">Whether we should use cached broadphase data. This is only valid during the physics step.</param>
        private void CreateProxies(PhysicsComponent body, FixturesComponent? manager = null, TransformComponent? xform = null)
        {
            if (!Resolve(body.OwnerUid, ref manager, ref xform) ||
                xform.MapID == MapId.Nullspace) return;

            var (worldPos, worldRot) = xform.GetWorldPositionRotation();

            // Outside of PVS (TODO Remove when PVS is better)
            if (float.IsNaN(worldPos.X) || float.IsNaN(worldPos.Y))
            {
                return;
            }

            var broadphase = GetBroadphase(body);

            if (broadphase == null)
            {
                throw new InvalidBroadphaseException($"Unable to find broadphase for {body.Owner}");
            }

            if (body.Broadphase != null)
            {
                throw new InvalidBroadphaseException($"{body.Owner} already has proxies on a broadphase?");
            }

            body.Broadphase = broadphase;

            foreach (var (_, fixture) in manager.Fixtures)
            {
                CreateProxies(fixture, worldPos, worldRot);
            }

            // Ensure cache remains up to date if the broadphase is moving.
            var uid = body.OwnerUid;

            if (EntityManager.TryGetComponent(uid, out BroadphaseComponent? broadphaseComp))
            {
                UpdateBroadphaseCache(broadphaseComp);
            }

            // Logger.DebugS("physics", $"Created proxies for {body.Owner} on {broadphase.Owner}");
        }

        /// <summary>
        /// Create the proxies for this fixture on the body's broadphase.
        /// </summary>
        internal void CreateProxies(Fixture fixture, Vector2 worldPos, Angle worldRot)
        {
            // Ideally we would always just defer this until Update / FrameUpdate but that will have to wait for a future
            // PR for my own sanity.

            DebugTools.Assert(fixture.ProxyCount == 0);
            DebugTools.Assert(fixture.Body.Owner.Transform.MapID != MapId.Nullspace);

            var proxyCount = fixture.Shape.ChildCount;

            if (proxyCount == 0) return;

            var broadphase = fixture.Body.Broadphase;

            if (broadphase == null)
            {
                throw new InvalidBroadphaseException($"Unable to find broadphase for create on {fixture.Body.Owner}");
            }

            fixture.ProxyCount = proxyCount;
            var proxies = fixture.Proxies;

            Array.Resize(ref proxies, proxyCount);
            fixture.Proxies = proxies;

            var broadphaseXform = EntityManager.GetComponent<TransformComponent>(broadphase.OwnerUid);

            var (broadphaseWorldPosition, broadphaseWorldRotation, broadphaseInvMatrix) = broadphaseXform.GetWorldPositionRotationInvMatrix();

            var localPos = broadphaseInvMatrix.Transform(worldPos);

            var transform = new Transform(localPos, worldRot - broadphaseWorldRotation);
            var mapId = broadphaseXform.MapID;

            for (var i = 0; i < proxyCount; i++)
            {
                var bounds = fixture.Shape.ComputeAABB(transform, i);
                var proxy = new FixtureProxy(bounds, fixture, i);
                proxy.ProxyId = broadphase.Tree.AddProxy(ref proxy);
                fixture.Proxies[i] = proxy;

                var worldAABB = new Box2Rotated(bounds, broadphaseWorldRotation, Vector2.Zero)
                    .CalcBoundingBox()
                    .Translated(broadphaseWorldPosition);

                AddToMoveBuffer(mapId, proxy, worldAABB);
            }
        }

        /// <summary>
        /// Destroy the proxies for this fixture on the broadphase.
        /// </summary>
        internal void DestroyProxies(BroadphaseComponent broadphase, Fixture fixture)
        {
            if (broadphase == null)
            {
                throw new InvalidBroadphaseException($"Unable to find broadphase for destroy on {fixture.Body}");
            }

            var proxyCount = fixture.ProxyCount;
            var moveBuffer = _moveBuffer[broadphase.Owner.Transform.MapID];

            for (var i = 0; i < proxyCount; i++)
            {
                var proxy = fixture.Proxies[i];
                broadphase.Tree.RemoveProxy(proxy.ProxyId);
                proxy.ProxyId = DynamicTree.Proxy.Free;
                moveBuffer.Remove(proxy);
            }

            fixture.ProxyCount = 0;
        }

        private void HandleContainerInsert(EntInsertedIntoContainerMessage ev)
        {
            if (ev.Entity.Deleted || !ev.Entity.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

            physicsComponent.CanCollide = false;
            physicsComponent.Awake = false;
        }

        private void HandleContainerRemove(EntRemovedFromContainerMessage ev)
        {
            if (ev.Entity.Deleted || !ev.Entity.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

            physicsComponent.CanCollide = true;
            physicsComponent.Awake = true;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.UnsubValueChanged(CVars.BroadphaseExpand, SetBroadphaseExpand);
            _mapManager.MapCreated -= OnMapCreated;
            _mapManager.MapDestroyed -= OnMapDestroyed;
        }

        #region Broadphase management

        private void OnMapCreated(object? sender, MapEventArgs e)
        {
            if (e.Map == MapId.Nullspace) return;

            EntityManager.EnsureComponent<BroadphaseComponent>(_mapManager.GetMapEntityId(e.Map));
            _moveBuffer[e.Map] = new Dictionary<FixtureProxy, Box2>(64);
        }

        private void OnMapDestroyed(object? sender, MapEventArgs e)
        {
            _moveBuffer.Remove(e.Map);
            _movedGrids.Remove(e.Map);
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            EntityManager.EnsureComponent<BroadphaseComponent>(ev.EntityUid);
        }

        private void HandleBroadphaseInit(EntityUid uid, BroadphaseComponent component, ComponentInit args)
        {
            var capacity = (int) Math.Max(MinimumBroadphaseCapacity, Math.Ceiling(component.Owner.Transform.ChildCount / (float) MinimumBroadphaseCapacity) * MinimumBroadphaseCapacity);
            component.Tree = new DynamicTreeBroadPhase(capacity);
        }

        #endregion

        internal BroadphaseComponent? GetBroadphase(PhysicsComponent body)
        {
            return GetBroadphase(body.Owner);
        }

        /// <summary>
        /// Attempt to get the relevant broadphase for this entity.
        /// Can return null if it's the map entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private BroadphaseComponent? GetBroadphase(IEntity entity)
        {
            if (entity.Transform.MapID == MapId.Nullspace)
            {
                return null;
            }

            // if it's map return null. Grids should return the map's broadphase.
            if (entity.HasComponent<BroadphaseComponent>() &&
                entity.Transform.Parent == null)
            {
                return null;
            }

            var parent = entity.Transform.Parent?.Owner;

            while (true)
            {
                if (parent == null) break;

                if (parent.TryGetComponent(out BroadphaseComponent? comp)) return comp;
                parent = parent.Transform.Parent?.Owner;
            }

            return null;
        }

        // TODO: The below is slow and should just query the map's broadphase directly. The problem is that
        // there's some ordering stuff going on where the broadphase has queued all of its updates but hasn't applied
        // them yet so this query will fail on initialization which chains into a whole lot of issues.
        internal IEnumerable<BroadphaseComponent> GetBroadphases(MapId mapId, Box2 aabb)
        {
            // TODO Okay so problem: If we just do Encloses that's a lot faster BUT it also means we don't return the
            // map's broadphase which avoids us iterating over it for 99% of bodies.

            if (mapId == MapId.Nullspace) yield break;

            foreach (var (broadphase, xform) in EntityManager.EntityQuery<BroadphaseComponent, TransformComponent>(true))
            {
                if (xform.MapID != mapId) continue;

                if (!EntityManager.TryGetComponent(broadphase.OwnerUid, out IMapGridComponent? mapGrid))
                {
                    yield return broadphase;
                    continue;
                }

                var grid = (IMapGridInternal) _mapManager.GetGrid(mapGrid.GridIndex);

                // Won't worry about accurate bounds checks as it's probably slower in most use cases.
                grid.GetMapChunks(aabb, out var chunkEnumerator);

                if (chunkEnumerator.MoveNext(out _))
                {
                    yield return broadphase;
                }
            }
        }

        private sealed class InvalidBroadphaseException : Exception
        {
            public InvalidBroadphaseException() {}

            public InvalidBroadphaseException(string message) : base(message) {}
        }
    }
}
