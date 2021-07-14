using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    public abstract class SharedBroadphaseSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private SharedPhysicsSystem _physicsSystem = default!;

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

        // TODO: Document the shit out of this madness internally. Also refactor DynamicTreeBroadphase.

        // We keep 2 move buffers as we need to handle the broadphase moving behavior first.
        // This is because we'll chuck anything the broadphase moves over onto the movebuffer so contacts can be generated.
        // TODO: These need to be FixtureProxy instead retard.
        private Dictionary<MapId, Dictionary<Fixture, Box2>> _moveBuffer = new();

        // Caching for FindNewContacts
        private Dictionary<FixtureProxy, HashSet<FixtureProxy>> _pairBuffer = new(64);
        private Dictionary<BroadphaseComponent, Vector2> _offsets = new(8);
        private Dictionary<BroadphaseComponent, Box2> _broadphaseBounding = new(8);
        private HashSet<EntityUid> _broadphases = new(8);
        private Dictionary<Fixture, Box2> _gridMoveBuffer = new(64);
        private List<FixtureProxy> _queryBuffer = new(32);

        // Caching for Synchronize
        private Dictionary<BroadphaseComponent, (Vector2 Position, float Rotation)> _broadphasePositions = new();

        public override void Initialize()
        {
            base.Initialize();
            UpdatesAfter.Add(typeof(SharedTransformSystem));
            _physicsSystem = Get<SharedPhysicsSystem>();

            SubscribeLocalEvent<BroadphaseComponent, ComponentInit>(HandleBroadphaseInit);
            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInit);
            SubscribeLocalEvent<BroadphaseComponent, ComponentShutdown>(HandleBroadphaseShutdown);

            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerInsert);
            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleContainerRemove);
            SubscribeLocalEvent<PhysicsUpdateMessage>(HandlePhysicsUpdate);

            // Shouldn't need to listen to mapchanges as parent changes should handle it...
            SubscribeLocalEvent<PhysicsComponent, EntParentChangedMessage>(HandleParentChange);

            SubscribeLocalEvent<PhysicsComponent, MoveEvent>(HandleMove);
            SubscribeLocalEvent<PhysicsComponent, RotateEvent>(HandleRotate);

            _mapManager.MapCreated += HandleMapCreated;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            ProcessUpdates();
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            //ProcessUpdates();
        }

        /// <summary>
        /// Go through every deferred event and update the broadphase.
        /// </summary>
        public void ProcessUpdates()
        {
            _handledThisTick.Clear();

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

                _handledThisTick.Add(update.Component.Owner.Uid);
            }

            // Body update may not necessarily handle this (unless the thing's deleted) so we'll still do this work regardless.
            while (_queuedParents.TryDequeue(out var parent))
            {
                if (parent.Entity.Deleted ||
                    !parent.Entity.TryGetComponent(out PhysicsComponent? body) ||
                    !body.CanCollide) continue;

                UpdateBroadphase(body);
                _handledThisTick.Add(body.Owner.Uid);
            }

            while (_queuedMoves.TryDequeue(out var move))
            {
                if (!_handledThisTick.Add(move.Sender.Uid) ||
                    move.Sender.Deleted ||
                    !move.Sender.TryGetComponent(out PhysicsComponent? body) ||
                    !body.CanCollide) continue;

                // TODO: Also need WorldRot here?
                var worldPos = move.NewPosition.ToMapPos(EntityManager);

                SynchronizeFixtures(body, worldPos);
            }

            while (_queuedRotates.TryDequeue(out var rotate))
            {
                if (!_handledThisTick.Add(rotate.Sender.Uid) ||
                    rotate.Sender.Deleted ||
                    !rotate.Sender.TryGetComponent(out PhysicsComponent? body) ||
                    !body.CanCollide) continue;

                var worldPos = rotate.Sender.Transform.WorldPosition;

                SynchronizeFixtures(body, worldPos);
            }
        }

        /// <summary>
        /// Check the AABB for each moved broadphase fixture and add any colliding entities to the movebuffer in case.
        /// </summary>
        private void FindGridContacts(MapId mapId)
        {
            // This is so that if we're on a broadphase that's moving (e.g. a grid) we need to make sure anything
            // we move over is getting checked for collisions, and putting it on the movebuffer is the easiest way to do so.
            var moveBuffer = _moveBuffer[mapId];

            // Rather than doing a HasComponent up front when adding to the moveBuffer we'll just do it here
            // This way we can reduce the amount of HasComponent<BroadphaseComponent> calls being done

            foreach (var (fixture, worldAABB) in moveBuffer)
            {
                if (!_broadphases.Contains(fixture.Body.Owner.Uid)) continue;

                var broadphase = fixture.Body.Broadphase!;
                var offset = _offsets[broadphase];
                var body = fixture.Body;
                var translatedAABB = worldAABB.Translated(-offset);

                // Easier to just not go over each proxy as we already unioned the fixture's worldaabb.
                foreach (var other in broadphase!.Tree.QueryAabb(_queryBuffer, translatedAABB))
                {
                    var otherFixture = other.Fixture;

                    // 99% of the time it's just going to be the broadphase (for now the grid) itself.
                    // hence this body check makes this run significantly better.
                    // Also check if it's not already on the movebuffer.
                    if (otherFixture.Body == body || moveBuffer.ContainsKey(otherFixture)) continue;

                    // To avoid updating during iteration.
                    _gridMoveBuffer[otherFixture] = other.AABB.Translated(offset);
                }

                _queryBuffer.Clear();
            }

            foreach (var (fixture, worldAABB) in _gridMoveBuffer)
            {
                moveBuffer[fixture] = worldAABB;
            }
        }

        internal void FindNewContacts(MapId mapId)
        {
            var moveBuffer = _moveBuffer[mapId];

            if (moveBuffer.Count == 0) return;

            // Cache as much broadphase data as we can up front for this map.
            foreach (var broadphase in ComponentManager.EntityQuery<BroadphaseComponent>(true))
            {
                var transform = broadphase.Owner.Transform;

                if (transform.MapID != mapId) continue;

                _broadphases.Add(broadphase.Owner.Uid);
                var worldPos = transform.WorldPosition;
                _offsets[broadphase] = worldPos;

                if (broadphase.Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
                {
                    _broadphaseBounding[broadphase] = physicsComponent.GetWorldAABB(worldPos);
                }
            }

            // Find any entities being driven over that might need to be considered
            FindGridContacts(mapId);

            // There is some mariana trench levels of bullshit going on.
            // We essentially need to re-create Box2D's FindNewContacts but in a way that allows us to check every
            // broadphase intersecting a particular proxy instead of just on the 1 broadphase.
            // This means we can generate contacts across different broadphases.
            // If you have a better way of allowing for broadphases attached to grids then by all means code it yourself.

            // FindNewContacts is inherently going to be a lot slower than Box2D's normal version so we need
            // to cache a bunch of stuff to make up for it.
            var contactManager = _physicsSystem.Maps[mapId].ContactManager;

            // TODO: Could store fixtures by broadphase for more perf
            foreach (var (fixture, worldAABB) in moveBuffer)
            {
                // Get every broadphase we may be intersecting.
                foreach (var (broadphase, offset) in _offsets)
                {
                    // Broadphase can't intersect with entities on itself so skip.
                    if (fixture.Body.Owner == broadphase.Owner) continue;

                    // If we're a map / our BB intersects then we'll do the work
                    if (_broadphaseBounding.TryGetValue(broadphase, out var broadphaseAABB) &&
                        !broadphaseAABB.Intersects(worldAABB)) continue;

                    foreach (var proxy in fixture.Proxies)
                    {
                        Box2 aabb;

                        // If it's the same broadphase as our body's one then don't need to translate the AABB.
                        if (proxy.Fixture.Body.Broadphase == broadphase)
                        {
                            aabb = proxy.AABB;
                        }
                        else
                        {
                            aabb = proxy.AABB
                                .Translated(_offsets[fixture.Body.Broadphase!])
                                .Translated(-offset);
                        }

                        foreach (var other in broadphase.Tree.QueryAabb(_queryBuffer, aabb))
                        {
                            // Do fast checks first and slower checks after (in ContactManager).
                            if (proxy == other ||
                                proxy.Fixture.Body == other.Fixture.Body ||
                                !ContactManager.ShouldCollide(proxy.Fixture, other.Fixture)) continue;

                            // Don't add duplicates.
                            // Look it disgusts me but we can't do it Box2D's way because we're getting pairs
                            // with different broadphases so can't use Proxy sorting to skip duplicates.
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
            }

            foreach (var (proxyA, proxies) in _pairBuffer)
            {
                foreach (var other in proxies)
                {
                    contactManager.AddPair(proxyA, other);
                }
            }

            _pairBuffer.Clear();
            _moveBuffer[mapId].Clear();
            _offsets.Clear();
            _broadphaseBounding.Clear();
            _broadphases.Clear();
            _gridMoveBuffer.Clear();
        }

        private void HandleBroadphaseShutdown(EntityUid uid, BroadphaseComponent component, ComponentShutdown args)
        {
            // TODO: Cleanup and remove all children.
        }

        private void HandleParentChange(EntityUid uid, PhysicsComponent component, EntParentChangedMessage args)
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
            CreateProxies(body);
        }

        /// <summary>
        /// Remove all of our fixtures from the broadphase.
        /// </summary>
        /// <param name="body"></param>
        private void DestroyProxies(PhysicsComponent body)
        {
            var broadphase = body.Broadphase;

            if (broadphase == null) return;

            foreach (var fixture in body._fixtures)
            {
                DestroyProxies(broadphase, fixture);
            }

            body.Broadphase = null;
        }

        private void HandlePhysicsUpdate(PhysicsUpdateMessage ev)
        {
            _queuedBodyUpdates.Enqueue(ev);
        }

        internal void AddBody(PhysicsComponent body)
        {
            // TODO: Good idea? Ehhhhhhhhhhhh
            // The problem is there's some fuckery with events while an entity is initializing.
            // Can probably just bypass this by doing stuff in Update / FrameUpdate again but future problem
            //
            if (body.Broadphase != null) return;

            CreateProxies(body);
        }

        internal void RemoveBody(PhysicsComponent body)
        {
            DestroyProxies(body);
        }

        public void RegenerateContacts(PhysicsComponent body)
        {
            var edge = body.ContactEdges;
            var contactManager = body.PhysicsMap.ContactManager;

            while (edge != null)
            {
                var ce0 = edge;
                edge = edge.Next;
                contactManager.Destroy(ce0.Contact!);
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
            // TODO: When movebuffer is changed to use proxies instead then update this mega hard
            var fixtureAABB = new Box2();

            foreach (var proxy in fixture.Proxies)
            {
                fixtureAABB = fixtureAABB.Union(proxy.AABB);
            }

            if (!fixtureAABB.IsEmpty())
                AddToMoveBuffer(mapId, fixture, fixtureAABB.Translated(broadphase.Owner.Transform.WorldPosition));
        }

        private void HandleMove(EntityUid uid, PhysicsComponent component, MoveEvent args)
        {
            _queuedMoves.Enqueue(args);
        }

        private void HandleRotate(EntityUid uid, PhysicsComponent component, RotateEvent args)
        {
            _queuedRotates.Enqueue(args);
        }

        public void CreateFixture(PhysicsComponent body, Fixture fixture)
        {
            fixture.ID = body.GetFixtureName(fixture);
            body._fixtures.Add(fixture);
            body.FixtureCount += 1;
            fixture.Body = body;

            // TODO: Assert world locked
            // Broadphase should be set in the future TM
            // Should only happen for nullspace / initializing entities
            if (body.Broadphase != null)
            {
                CreateProxies(fixture, body.Owner.Transform.WorldPosition);
            }

            // Supposed to be wrapped in density but eh
            body.ResetMassData();
            // TODO: Set newcontacts to true.
        }

        public void DestroyFixture(PhysicsComponent body, Fixture fixture)
        {
            // TODO: Assert world locked
            DebugTools.Assert(fixture.Body == body);
            DebugTools.Assert(body.FixtureCount > 0);

            if (!body._fixtures.Remove(fixture))
            {
                DebugTools.Assert(false);
                // TODO: Log
                return;
            }

            var edge = body.ContactEdges;

            while (edge != null)
            {
                var contact = edge.Contact!;
                edge = edge.Next;

                var fixtureA = contact.FixtureA;
                var fixtureB = contact.FixtureB;

                if (fixture == fixtureA || fixture == fixtureB)
                {
                    body.PhysicsMap.ContactManager.Destroy(contact);
                }
            }

            var broadphase = GetBroadphase(fixture.Body);

            if (body.CanCollide && broadphase != null)
            {
                DestroyProxies(broadphase, fixture);
            }

            body.FixtureCount -= 1;
            body.ResetMassData();
        }

        private void SynchronizeFixtures(PhysicsComponent body, Vector2 worldPos)
        {
            // Logger.DebugS("physics", $"Synchronizing fixtures for {body.Owner}");
            var xf = new Transform(worldPos, (float) body.Owner.Transform.WorldRotation.Theta);

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
        }

        /// <summary>
        /// The standard Synchronize from Box2D
        /// </summary>
        private void SynchronizeTOI(Fixture fixture, Transform transform1, Transform transform2)
        {
            var broadphase = fixture.Body.Broadphase!;
            var proxyCount = fixture.ProxyCount;

            var fixtureAABB = new Box2(transform1.Position, transform1.Position);
            // TODO: Inefficient as fuck
            var broadphaseTransform = broadphase.Owner.Transform;

            if (!_broadphasePositions.TryGetValue(broadphase, out var broadphaseOffset))
            {
                broadphaseOffset = (broadphaseTransform.WorldPosition, (float) broadphaseTransform.WorldRotation.Theta);
                _broadphasePositions[broadphase] = broadphaseOffset;
            }

            var relativePos1 = new Transform(transform1.Position - broadphaseOffset.Position,
                transform1.Quaternion2D.Angle - broadphaseOffset.Rotation);

            var relativePos2 = new Transform(transform2.Position - broadphaseOffset.Position,
                transform2.Quaternion2D.Angle - broadphaseOffset.Rotation);

            var angle1 = new Angle(relativePos1.Quaternion2D.Angle);
            var angle2 = new Angle(relativePos2.Quaternion2D.Angle);

            for (var i = 0; i < proxyCount; i++)
            {
                 var proxy = fixture.Proxies[i];

                 var aabb1 = fixture.Shape.CalculateLocalBounds(angle1).Translated(relativePos1.Position);
                 var aabb2 = fixture.Shape.CalculateLocalBounds(angle2).Translated(relativePos2.Position);

                 var aabb = aabb1.Union(aabb2);
                 proxy.AABB = aabb;
                 var displacement = aabb2.Center - aabb1.Center;
                 broadphase.Tree.MoveProxy(proxy.ProxyId, aabb, displacement);
                 var worldAABB = proxy.AABB.Translated(broadphaseOffset.Position);
                 fixtureAABB = proxyCount > 1 ? fixtureAABB.Union(worldAABB) : worldAABB;
            }

            AddToMoveBuffer(broadphaseTransform.MapID, fixture, fixtureAABB);
        }

        /// <summary>
        /// A more efficient Synchronize for 1 transform.
        /// </summary>
        private void Synchronize(Fixture fixture, Transform transform1)
        {
            // tl;dr update our bounding boxes stored in broadphase.
            var broadphase = fixture.Body.Broadphase!;
            var proxyCount = fixture.ProxyCount;

            // TODO: Inefficient as fuck
            var broadphaseTransform = broadphase.Owner.Transform;

            if (!_broadphasePositions.TryGetValue(broadphase, out var broadphaseOffset))
            {
                broadphaseOffset = (broadphaseTransform.WorldPosition, (float) broadphaseTransform.WorldRotation.Theta);
                _broadphasePositions[broadphase] = broadphaseOffset;
            }

            var relativePos1 = new Transform(transform1.Position - broadphaseOffset.Position,
                transform1.Quaternion2D.Angle - broadphaseOffset.Rotation);

            var angle1 = new Angle(relativePos1.Quaternion2D.Angle);

            var fixtureAABB = new Box2(transform1.Position, transform1.Position);

            for (var i = 0; i < proxyCount; i++)
            {
                var proxy = fixture.Proxies[i];
                var aabb = fixture.Shape.CalculateLocalBounds(angle1).Translated(relativePos1.Position);
                proxy.AABB = aabb;
                var displacement = Vector2.Zero;
                broadphase.Tree.MoveProxy(proxy.ProxyId, aabb, displacement);
                var worldAABB = proxy.AABB.Translated(broadphaseOffset.Position);
                fixtureAABB = proxyCount > 1 ? fixtureAABB.Union(worldAABB) : worldAABB;
            }

            AddToMoveBuffer(broadphaseTransform.MapID, fixture, fixtureAABB);
        }

        private void AddToMoveBuffer(MapId mapId, Fixture fixture, Box2 aabb)
        {
            _moveBuffer[mapId][fixture] = aabb;
        }

        private void CreateProxies(PhysicsComponent body)
        {
            if (body.Owner.Transform.MapID == MapId.Nullspace) return;

            var worldPos = body.Owner.Transform.WorldPosition;

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
                CreateProxies(fixture, worldPos);
            }

            // Logger.DebugS("physics", $"Created proxies for {body.Owner} on {broadphase.Owner}");
        }

        /// <summary>
        /// Create the proxies for this fixture on the body's broadphase.
        /// </summary>
        private void CreateProxies(Fixture fixture, Vector2 worldPos)
        {
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

            // TODO: Cache above once stable
            var broadphasePos = broadphase.Owner.Transform.WorldPosition;
            var broadphaseRot = broadphase.Owner.Transform.WorldRotation;

            var worldRot = fixture.Body.Owner.Transform.WorldRotation;

            var posDiff = worldPos - broadphasePos;
            var rotDiff = worldRot - broadphaseRot;

            var fixtureAABB = new Box2(worldPos, worldPos);

            for (var i = 0; i < proxyCount; i++)
            {
                var aabb = fixture.Shape.CalculateLocalBounds(rotDiff).Translated(posDiff);
                var proxy = new FixtureProxy(aabb, fixture, i);
                proxy.ProxyId = broadphase.Tree.AddProxy(ref proxy);
                fixture.Proxies[i] = proxy;
                fixtureAABB = fixtureAABB.Union(aabb).Translated(broadphasePos);
            }

            AddToMoveBuffer(broadphase.Owner.Transform.MapID, fixture, fixtureAABB);
        }

        /// <summary>
        /// Destroy the proxies for this fixture on the broadphase.
        /// </summary>
        private void DestroyProxies(BroadphaseComponent broadphase, Fixture fixture)
        {
            if (broadphase == null)
            {
                throw new InvalidBroadphaseException($"Unable to find broadphase for destroy on {fixture.Body}");
            }

            var proxyCount = fixture.ProxyCount;

            for (var i = 0; i < proxyCount; i++)
            {
                var proxy = fixture.Proxies[i];
                broadphase.Tree.RemoveProxy(proxy.ProxyId);
                proxy.ProxyId = DynamicTree.Proxy.Free;
            }

            var mapId = broadphase.Owner.Transform.MapID;
            _moveBuffer[mapId].Remove(fixture);

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

        private void HandleMapCreated(object? sender, MapEventArgs e)
        {
            if (e.Map == MapId.Nullspace) return;

            var mapEnt = _mapManager.GetMapEntity(e.Map);
            mapEnt.EnsureComponent<BroadphaseComponent>();
            _moveBuffer[e.Map] = new Dictionary<Fixture, Box2>(64);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.MapCreated -= HandleMapCreated;
            // TODO: Destroy buffers here
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            var grid = EntityManager.GetEntity(ev.EntityUid);
            grid.EnsureComponent<BroadphaseComponent>();
        }

        private void HandleBroadphaseInit(EntityUid uid, BroadphaseComponent component, ComponentInit args)
        {
            var capacity = (int) Math.Max(MinimumBroadphaseCapacity, Math.Ceiling(component.Owner.Transform.ChildCount / (float) MinimumBroadphaseCapacity) * MinimumBroadphaseCapacity);
            component.Tree = new DynamicTreeBroadPhase(capacity);
        }

        private BroadphaseComponent? GetBroadphase(PhysicsComponent body)
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

        #region Queries
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

        // TODO: The below is slow and should just query the map's broadphase directly. The problem is that
        // there's some ordering stuff going on where the broadphase has queued all of its updates but hasn't applied
        // them yet so this query will fail on initialization which chains into a whole lot of issues.
        internal IEnumerable<BroadphaseComponent> GetBroadphases(MapId mapId, Box2 aabb)
        {
            // TODO Okay so problem: If we just do Encloses that's a lot faster BUT it also means we don't return the
            // map's broadphase which avoids us iterating over it for 99% of bodies.

            if (mapId == MapId.Nullspace) yield break;

            foreach (var broadphase in ComponentManager.EntityQuery<BroadphaseComponent>(true))
            {
                if (broadphase.Owner.Transform.MapID != mapId) continue;
                if (broadphase.Owner.HasComponent<MapComponent>())
                {
                    yield return broadphase;
                    continue;
                }

                if (broadphase.Owner.TryGetComponent(out PhysicsComponent? physicsComponent) &&
                    Intersects(physicsComponent, aabb))
                {
                    yield return broadphase;
                }
            }
        }

        internal IEnumerable<BroadphaseComponent> GetBroadphases(MapId mapId, Vector2 worldPos)
        {
            return GetBroadphases(mapId, new Box2(worldPos, worldPos));
        }

        private bool Intersects(PhysicsComponent physicsComponent, Box2 aabb)
        {
            var worldPos = physicsComponent.Owner.Transform.WorldPosition;
            var worldRot = physicsComponent.Owner.Transform.WorldRotation;
            var bodyAABB = physicsComponent.GetWorldAABB(worldPos, worldRot);

            if (!aabb.Intersects(bodyAABB)) return false;

            foreach (var fixture in physicsComponent.Fixtures)
            {
                if (fixture.Shape.Intersects(aabb, worldPos, worldRot)) return true;
            }

            return false;
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

            foreach (var broadphase in GetBroadphases(mapId, collider))
            {
                // TODO: Is rotation a problem here?
                var gridCollider = collider.Translated(-broadphase.Owner.Transform.WorldPosition);

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

            foreach (var fixture in body._fixtures)
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

            foreach (var broadphase in GetBroadphases(mapId, worldAABB))
            {
                var gridAABB = worldAABB.Translated(-broadphase.Owner.Transform.WorldPosition);

                foreach (var proxy in broadphase.Tree.QueryAabb(gridAABB, false))
                {
                    bodies.Add(proxy.Fixture.Body);
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
        #endregion

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

            foreach (var broadphase in GetBroadphases(mapId, rayBox))
            {
                var offset = broadphase.Owner.Transform.WorldPosition;

                var gridRay = new CollisionRay(ray.Position - offset, ray.Direction, ray.CollisionMask);
                // TODO: Probably need rotation when we get rotatable grids

                broadphase.Tree.QueryRay((in FixtureProxy proxy, in Vector2 point, float distFromOrigin) =>
                {
                    if (returnOnFirstHit && results.Count > 0) return true;

                    if (distFromOrigin > maxLength)
                    {
                        return true;
                    }

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
                    var result = new RayCastResults(distFromOrigin, point + offset, proxy.Fixture.Body.Owner);
                    results.Add(result);
                    EntityManager.EventBus.QueueEvent(EventSource.Local,
                        new DebugDrawRayMessage(
                            new DebugRayData(ray, maxLength, result)));
                    return true;
                }, gridRay);
            }

            if (results.Count == 0)
            {
                EntityManager.EventBus.QueueEvent(EventSource.Local,
                    new DebugDrawRayMessage(
                        new DebugRayData(ray, maxLength, null)));
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

            foreach (var broadphase in GetBroadphases(mapId, rayBox))
            {
                var offset = broadphase.Owner.Transform.WorldPosition;

                var gridRay = new CollisionRay(ray.Position - offset, ray.Direction, ray.CollisionMask);
                // TODO: Probably need rotation when we get rotatable grids

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

            return penetration;
        }
        #endregion

        private sealed class InvalidBroadphaseException : Exception
        {
            public InvalidBroadphaseException() {}

            public InvalidBroadphaseException(string message) : base(message) {}
        }
    }
}
