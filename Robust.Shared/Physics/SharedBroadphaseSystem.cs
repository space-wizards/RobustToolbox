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
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private const int MinimumBroadphaseCapacity = 256;

        // We queue updates rather than handle them immediately for multiple reasons
        // A) Entity initializing may call several events which only need handling once so we'd need to add a bunch of code to account for what stage of initializing they're at
        // B) It's faster for instances like MoveEvent and RotateEvent both being issued

        private Queue<PhysicsUpdateMessage> _queuedBodyUpdates = new();
        private Queue<MoveEvent> _queuedMoves = new();
        private Queue<RotateEvent> _queuedRotates = new();
        private Queue<EntParentChangedMessage> _queuedParents = new();

        /// <summary>
        /// To avoid duplicating work we'll keep a track of what we've already updated in the broadphase.
        /// </summary>
        private HashSet<EntityUid> _handledThisTick = new();

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
            SubscribeLocalEvent<PhysicsUpdateMessage>(HandlePhysicsUpdate);

            // Shouldn't need to listen to mapchanges as parent changes should handle it...
            SubscribeLocalEvent<PhysicsComponent, EntParentChangedMessage>(HandleParentChange);

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
            _handledThisTick.Clear();
            EnsureBroadphaseTransforms();

            while (_queuedBodyUpdates.TryDequeue(out var update))
            {
                if (update.Component.Deleted || !update.Component.CanCollide)
                {
                    RemoveBody(update.Component);
                }
                else
                {
                    AddBody(update.Component);
                }

                _handledThisTick.Add(((IComponent) update.Component).Owner);
            }

            // Body update may not necessarily handle this (unless the thing's deleted) so we'll still do this work regardless.
            while (_queuedParents.TryDequeue(out var parent))
            {
                if ((!_entityManager.EntityExists(parent.Entity) ? EntityLifeStage.Deleted : _entityManager.GetComponent<MetaDataComponent>(parent.Entity).EntityLifeStage) >= EntityLifeStage.Deleted ||
                    !_entityManager.TryGetComponent(parent.Entity, out PhysicsComponent? body) ||
                    !body.CanCollide) continue;

                UpdateBroadphase(body);
                _handledThisTick.Add(((IComponent) body).Owner);
            }

            while (_queuedMoves.TryDequeue(out var move))
            {
                if ((!_entityManager.EntityExists(move.Sender) ? EntityLifeStage.Deleted : _entityManager.GetComponent<MetaDataComponent>(move.Sender).EntityLifeStage) >= EntityLifeStage.Deleted ||
                    !_entityManager.TryGetComponent(move.Sender, out PhysicsComponent? body) ||
                    !body.CanCollide) continue;

                var worldPos = move.NewPosition.ToMapPos(EntityManager);
                var worldRot = (float) _entityManager.GetComponent<TransformComponent>(move.Sender).WorldRotation.Theta;

                SynchronizeFixtures(body, worldPos, worldRot);
            }

            while (_queuedRotates.TryDequeue(out var rotate))
            {
                if (!_handledThisTick.Add(rotate.Sender) ||
                    (!_entityManager.EntityExists(rotate.Sender) ? EntityLifeStage.Deleted : _entityManager.GetComponent<MetaDataComponent>(rotate.Sender).EntityLifeStage) >= EntityLifeStage.Deleted ||
                    !_entityManager.TryGetComponent(rotate.Sender, out PhysicsComponent? body) ||
                    !body.CanCollide) continue;

                var worldPos = _entityManager.GetComponent<TransformComponent>(rotate.Sender).WorldPosition;
                var worldRot = (float) rotate.NewRotation.Theta;

                SynchronizeFixtures(body, worldPos, worldRot);
            }

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
            var uid = ((IComponent) broadphase).Owner;

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
            EntityUid tempQualifier = _mapManager.GetMapEntityId(mapId, true);
            var contactManager = _entityManager.GetComponent<SharedPhysicsMapComponent>(tempQualifier).ContactManager;

            // TODO: Could store fixtures by broadphase for more perf?
            foreach (var (proxy, worldAABB) in moveBuffer)
            {
                var proxyBody = proxy.Fixture.Body;
                // if (prediction && !proxyBody.Predict) continue;

                // Get every broadphase we may be intersecting.
                foreach (var (broadphase, _) in _broadphaseTransforms)
                {
                    // Broadphase can't intersect with entities on itself so skip.
                    if (((IComponent) proxyBody).Owner == ((IComponent) broadphase).Owner ||
                        _entityManager.GetComponent<TransformComponent>(broadphase.Owner).MapID != _entityManager.GetComponent<TransformComponent>(proxyBody.Owner).MapID) continue;

                    var enlargedAABB = worldAABB.Enlarged(_broadphaseExpand);

                    // If we're a map / our BB intersects then we'll do the work
                    if (_broadphaseBounding.TryGetValue(((IComponent) broadphase).Owner, out var broadphaseAABB) &&
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
                        aabb = _broadphaseInvMatrices[((IComponent) broadphase).Owner].TransformBox(worldAABB);
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

        private void HandleParentChange(EntityUid uid, PhysicsComponent component, ref EntParentChangedMessage args)
        {
            _queuedParents.Enqueue(args);
        }

        /// <summary>
        /// If our broadphase has changed then remove us from our old one and add to our new one.
        /// </summary>
        /// <param name="body"></param>
        private void UpdateBroadphase(PhysicsComponent body)
        {
            var oldBroadphase = body.Broadphase;
            var newBroadphase = GetBroadphase(body);

            if (oldBroadphase == newBroadphase) return;

            DestroyProxies(body);

            // Shouldn't need to null-check as this already checks for nullspace so should be okay...?
            CreateProxies(body, true);
        }

        /// <summary>
        /// Remove all of our fixtures from the broadphase.
        /// </summary>
        private void DestroyProxies(PhysicsComponent body, FixturesComponent? manager = null)
        {
            if (!Resolve(((IComponent) body).Owner, ref manager)) return;

            var broadphase = body.Broadphase;

            if (broadphase == null) return;

            foreach (var (_, fixture) in manager.Fixtures)
            {
                DestroyProxies(broadphase, fixture);
            }

            body.Broadphase = null;
        }

        private void HandlePhysicsUpdate(PhysicsUpdateMessage ev)
        {
            _queuedBodyUpdates.Enqueue(ev);
        }

        private void AddBody(PhysicsComponent body)
        {
            // TODO: Good idea? Ehhhhhhhhhhhh
            // The problem is there's some fuckery with events while an entity is initializing.
            // Can probably just bypass this by doing stuff in Update / FrameUpdate again but future problem
            //
            if (body.Broadphase != null) return;

            CreateProxies(body, true);
        }

        internal void RemoveBody(PhysicsComponent body, FixturesComponent? manager = null)
        {
            // TODO: Would reaaalllyy like for this to not be false in future
            if (!Resolve(((IComponent) body).Owner, ref manager, false))
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
                var mapId = _entityManager.GetComponent<TransformComponent>(body.Owner).MapID;

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

            TouchProxies(_entityManager.GetComponent<TransformComponent>(fixture.Body.Owner).MapID, broadphase, fixture);
        }

        private void TouchProxies(MapId mapId, BroadphaseComponent broadphase, Fixture fixture)
        {
            var broadphasePos = _entityManager.GetComponent<TransformComponent>(broadphase.Owner).WorldPosition;

            foreach (var proxy in fixture.Proxies)
            {
                AddToMoveBuffer(mapId, proxy, proxy.AABB.Translated(broadphasePos));
            }
        }

        private void HandleMove(EntityUid uid, PhysicsComponent component, ref MoveEvent args)
        {
            _queuedMoves.Enqueue(args);
        }

        private void HandleRotate(EntityUid uid, PhysicsComponent component, ref RotateEvent args)
        {
            _queuedRotates.Enqueue(args);
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
            var uid = ((IComponent) body).Owner;

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

            var broadphaseMapId = _entityManager.GetComponent<TransformComponent>(broadphase.Owner).MapID;
            var broadphaseInvMatrix = _broadphaseInvMatrices[((IComponent) broadphase).Owner];
            var broadphaseXform = _broadphaseTransforms[broadphase];

            var relativePos1 = new Transform(
                broadphaseInvMatrix.Transform(transform1.Position),
                transform1.Quaternion2D.Angle - broadphaseXform.Rotation);

            for (var i = 0; i < proxyCount; i++)
            {
                var proxy = fixture.Proxies[i];
                var bounds = fixture.Shape.ComputeAABB(relativePos1, i);
                proxy.AABB = bounds;
                var displacement = Vector2.Zero;
                broadphase.Tree.MoveProxy(proxy.ProxyId, bounds, displacement);

                var worldAABB = new Box2Rotated(bounds, broadphaseXform.Rotation, Vector2.Zero)
                    .CalcBoundingBox()
                    .Translated(broadphaseXform.Position);

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
        /// <param name="body">The body to update the proxies for.</param>
        /// <param name="useCache">Whether we should use cached broadphase data. This is only valid during the physics step.</param>
        /// <exception cref="InvalidBroadphaseException"></exception>
        private void CreateProxies(PhysicsComponent body, bool useCache)
        {
            if (_entityManager.GetComponent<TransformComponent>(body.Owner).MapID == MapId.Nullspace) return;

            var worldPos = _entityManager.GetComponent<TransformComponent>(body.Owner).WorldPosition;
            var worldRot = _entityManager.GetComponent<TransformComponent>(body.Owner).WorldRotation;

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

            foreach (var fixture in body.Fixtures)
            {
                CreateProxies(fixture, worldPos, worldRot, useCache);
            }

            // Ensure cache remains up to date if the broadphase is moving.
            var uid = ((IComponent) body).Owner;

            if (EntityManager.TryGetComponent(uid, out BroadphaseComponent? broadphaseComp))
            {
                UpdateBroadphaseCache(broadphaseComp);
            }

            // Logger.DebugS("physics", $"Created proxies for {body.Owner} on {broadphase.Owner}");
        }

        /// <summary>
        /// Create the proxies for this fixture on the body's broadphase.
        /// </summary>
        internal void CreateProxies(Fixture fixture, Vector2 worldPos, Angle worldRot, bool useCache)
        {
            // Ideally we would always just defer this until Update / FrameUpdate but that will have to wait for a future
            // PR for my own sanity.

            DebugTools.Assert(fixture.ProxyCount == 0);
            DebugTools.Assert(_entityManager.GetComponent<TransformComponent>(fixture.Body.Owner).MapID != MapId.Nullspace);

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

            Matrix3 broadphaseInvMatrix;
            (Vector2 Position, float Rotation) broadphaseTransform;
            var xform = EntityManager.GetComponent<TransformComponent>(((IComponent) broadphase).Owner);

            if (useCache)
            {
                broadphaseInvMatrix = _broadphaseInvMatrices[((IComponent) broadphase).Owner];
                broadphaseTransform = _broadphaseTransforms[broadphase];
            }
            else
            {
                Vector2 wp;
                Angle wr;

                (wp, wr, broadphaseInvMatrix) = xform.GetWorldPositionRotationInvMatrix();

                broadphaseTransform = (wp, (float) wr.Theta);
            }

            var localPos = broadphaseInvMatrix.Transform(worldPos);

            var transform = new Transform(localPos, worldRot - broadphaseTransform.Rotation);
            var mapId = xform.MapID;

            for (var i = 0; i < proxyCount; i++)
            {
                var bounds = fixture.Shape.ComputeAABB(transform, i);
                var proxy = new FixtureProxy(bounds, fixture, i);
                proxy.ProxyId = broadphase.Tree.AddProxy(ref proxy);
                fixture.Proxies[i] = proxy;

                var worldAABB = new Box2Rotated(bounds, broadphaseTransform.Rotation, Vector2.Zero)
                    .CalcBoundingBox()
                    .Translated(broadphaseTransform.Position);

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
            var moveBuffer = _moveBuffer[_entityManager.GetComponent<TransformComponent>(broadphase.Owner).MapID];

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
            if ((!_entityManager.EntityExists(ev.Entity) ? EntityLifeStage.Deleted : _entityManager.GetComponent<MetaDataComponent>(ev.Entity).EntityLifeStage) >= EntityLifeStage.Deleted || !_entityManager.TryGetComponent(ev.Entity, out PhysicsComponent? physicsComponent)) return;

            physicsComponent.CanCollide = false;
            physicsComponent.Awake = false;
        }

        private void HandleContainerRemove(EntRemovedFromContainerMessage ev)
        {
            if ((!_entityManager.EntityExists(ev.Entity) ? EntityLifeStage.Deleted : _entityManager.GetComponent<MetaDataComponent>(ev.Entity).EntityLifeStage) >= EntityLifeStage.Deleted || !_entityManager.TryGetComponent(ev.Entity, out PhysicsComponent? physicsComponent)) return;

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
            var capacity = (int) Math.Max(MinimumBroadphaseCapacity, Math.Ceiling(_entityManager.GetComponent<TransformComponent>(component.Owner).ChildCount / (float) MinimumBroadphaseCapacity) * MinimumBroadphaseCapacity);
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
        /// <param name="transform"></param>
        /// <returns></returns>
        private BroadphaseComponent? GetBroadphase(EntityUid entity, TransformComponent? transform = null)
        {
            if (!Resolve(entity, ref transform))
                return null;

            if (transform.MapID == MapId.Nullspace)
            {
                return null;
            }

            // if it's map return null. Grids should return the map's broadphase.
            if (_entityManager.HasComponent<BroadphaseComponent>(entity) &&
                transform.Parent == null)
            {
                return null;
            }

            var parent = transform.Parent;

            while (true)
            {
                if (parent == null) break;

                if (_entityManager.TryGetComponent(parent.OwnerUid, out BroadphaseComponent? comp)) return comp;
                parent = parent.Parent;
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

                if (!EntityManager.TryGetComponent(((IComponent) broadphase).Owner, out IMapGridComponent? mapGrid))
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
