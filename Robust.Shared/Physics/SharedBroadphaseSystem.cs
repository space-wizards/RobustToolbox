using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
        private Dictionary<MapId, Dictionary<Fixture, Box2>> _broadphaseMoveBuffer = new();
        private Dictionary<MapId, Dictionary<Fixture, Box2>> _moveBuffer = new();

        public override void Initialize()
        {
            base.Initialize();
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
            ProcessUpdates();
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

                SynchronizeFixtures(body);
            }

            while (_queuedRotates.TryDequeue(out var move))
            {
                if (!_handledThisTick.Add(move.Sender.Uid) ||
                    move.Sender.Deleted ||
                    !move.Sender.TryGetComponent(out PhysicsComponent? body) ||
                    !body.CanCollide) continue;

                SynchronizeFixtures(body);
            }
        }

        /// <summary>
        /// Check the AABB for each moved broadphase fixture and add any colliding entities to the movebuffer in case.
        /// </summary>
        /// <param name="mapId"></param>
        private void FindGridContacts(MapId mapId)
        {
            // This is so that if we're on a broadphase that's moving (e.g. a grid) we need to make sure anything
            // we move over is getting checked for collisions, and putting it on the movebuffer is the easiest way to do so.

            // TODO: Perf problems here too

            var moveBuffer = _moveBuffer[mapId];

            foreach (var (fixture, worldAABB) in _broadphaseMoveBuffer[mapId])
            {
                var broadphase = GetBroadphase(fixture.Body);
                var offset = broadphase!.Owner.Transform.WorldPosition;

                foreach (var proxy in fixture.Proxies)
                {
                    foreach (var other in broadphase!.Tree.QueryAabb(worldAABB.Translated(-offset)))
                    {
                        var otherFixture = other.Fixture;

                        if (other == proxy || moveBuffer.ContainsKey(otherFixture)) continue;

                        moveBuffer[otherFixture] = other.AABB.Translated(offset);
                    }
                }
            }

            _broadphaseMoveBuffer[mapId].Clear();
        }

        internal void FindNewContacts(MapId mapId)
        {
            FindGridContacts(mapId);

            if (_moveBuffer[mapId].Count == 0) return;

            // There is some mariana trench levels of bullshit going on.
            // We essentially need to re-create Box2D's FindNewContacts but in a way that allows us to check every
            // broadphase intersecting a particular proxy instead of just on the 1 broadphase.
            // This means we can generate contacts across different broadphases.
            // If you have a better way of allowing for broadphases attached to grids then by all means code it yourself.

            // TODO: Need to fuck around with optimising this a lot.
            var offsets = new Dictionary<BroadphaseComponent, Vector2>();
            var contactManagers = new Dictionary<BroadphaseComponent, ContactManager>();

            foreach (var broadphase in ComponentManager.EntityQuery<BroadphaseComponent>(true))
            {
                var transform = broadphase.Owner.Transform;

                if (transform.MapID != mapId) continue;

                offsets[broadphase] = transform.WorldPosition;
                contactManagers[broadphase] = Get<SharedPhysicsSystem>().Maps[transform.MapID].ContactManager;
            }

            // TODO: Could store fixtures by broadphase for more perf
            // TODO: put this as a member field.
            var pairs = new List<(FixtureProxy, FixtureProxy)>();

            foreach (var (fixture, worldAABB) in _moveBuffer[mapId])
            {
                // Get every broadphase we may be intersecting.
                foreach (var broadphase in GetBroadphases(fixture.Body.Owner.Transform.MapID, worldAABB))
                {
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
                                .Translated(offsets[fixture.Body.Broadphase!])
                                .Translated(-offsets[broadphase]);
                        }

                        // TODO: Approx or not?
                        foreach (var other in broadphase.Tree.QueryAabb(aabb, false))
                        {
                            if (proxy == other || !ContactManager.ShouldCollide(proxy.Fixture, other.Fixture)) continue;

                            pairs.Add((proxy, other));
                        }
                    }
                }
            }

            foreach (var (proxyA, proxyB) in pairs)
            {
                var contactManager = contactManagers[proxyA.Fixture.Body.Broadphase!];
                contactManager.AddPair(proxyA, proxyB);
            }

            _moveBuffer[mapId].Clear();
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
                foreach (var fixture in body.Fixtures)
                {
                    var proxyCount = fixture.ProxyCount;

                    for (var i = 0; i < proxyCount; i++)
                    {
                        broadphase.Tree.TouchProxy(fixture.Proxies[i].ProxyId);
                    }
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
                    // TODO: Flag for filtering
                }

                edge = edge.Next;
            }

            var broadphase = body.Broadphase;

            // If nullspace or whatever ignore it.
            if (broadphase == null) return;

            for (var i = 0; i < fixture.ProxyCount; i++)
            {
                broadphase.Tree.TouchProxy(fixture.Proxies[i].ProxyId);
            }
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
            // TODO: Assert world locked
            // Broadphase should be set in the future TM
            // Should only happen for nullspace / initializing entities
            if (body.Broadphase != null)
            {
                CreateProxies(fixture, body.Owner.Transform.WorldPosition);
            }

            body._fixtures.Add(fixture);
            body.FixtureCount += 1;
            fixture.Body = body;

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

        private void SynchronizeFixtures(PhysicsComponent body)
        {
            // Logger.DebugS("physics", $"Synchronizing fixtures for {body.Owner}");

            if (body.Awake)
            {
                // TODO: SWEPT HERE
                var xf = body.GetTransform();

                foreach (var fixture in body.Fixtures)
                {
                    Synchronize(fixture, xf, xf);
                }
            }
            else
            {
                var xf = body.GetTransform();

                foreach (var fixture in body.Fixtures)
                {
                    Synchronize(fixture, xf, xf);
                }
            }
        }

        private void Synchronize(Fixture fixture, Transform transform1, Transform transform2)
        {
            var proxyCount = fixture.ProxyCount;

            if (proxyCount == 0) return;

            var broadphase = fixture.Body.Broadphase;

            if (broadphase == null)
            {
                throw new InvalidBroadphaseException($"Unable to find broadphase for Synchronize for {fixture.Body}");
            }

            var fixtureAABB = new Box2(transform1.Position, transform1.Position);
            // TODO: Inefficient as fuck
            var broadphaseTransform = broadphase.Owner.Transform;
            var broadphaseOffset = broadphaseTransform.WorldPosition;
            var broadphaseRot = broadphaseTransform.WorldRotation;

            var relativePos1 = new Transform(transform1.Position - broadphaseOffset,
                transform1.Quaternion2D.Angle - (float) broadphaseRot.Theta);

            var relativePos2 = new Transform(transform2.Position - broadphaseOffset,
                transform2.Quaternion2D.Angle - (float) broadphaseRot.Theta);

            for (var i = 0; i < proxyCount; i++)
            {
                var proxy = fixture.Proxies[i];

                var aabb1 = fixture.Shape.CalculateLocalBounds(relativePos1.Quaternion2D.Angle).Translated(relativePos1.Position);
                var aabb2 = fixture.Shape.CalculateLocalBounds(relativePos2.Quaternion2D.Angle).Translated(relativePos2.Position);

                proxy.AABB = aabb1.Union(aabb2);
                var displacement = aabb2.Center - aabb1.Center;

                broadphase.Tree.MoveProxy(proxy.ProxyId, proxy.AABB, displacement);
                fixtureAABB = fixtureAABB.Union(proxy.AABB.Translated(broadphaseOffset));
            }

            _moveBuffer[broadphaseTransform.MapID][fixture] = fixtureAABB;

            if (fixture.Body.Owner.HasComponent<BroadphaseComponent>())
            {
                _broadphaseMoveBuffer[broadphaseTransform.MapID][fixture] = fixtureAABB;
            }
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

            _moveBuffer[broadphase.Owner.Transform.MapID][fixture] = fixtureAABB;

            if (fixture.Body.Owner.HasComponent<BroadphaseComponent>())
            {
                _broadphaseMoveBuffer[broadphase.Owner.Transform.MapID][fixture] = fixtureAABB;
            }
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

            _moveBuffer[broadphase.Owner.Transform.MapID].Remove(fixture);
            // TODO: Check HasComponent maybe? Just wanted to prevent leaks juusssttt in case.
            _broadphaseMoveBuffer[broadphase.Owner.Transform.MapID].Remove(fixture);

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
            _broadphaseMoveBuffer[e.Map] = new Dictionary<Fixture, Box2>(64);
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

        internal IEnumerable<BroadphaseComponent> GetBroadphases(MapId mapId, Box2 aabb)
        {
            if (mapId == MapId.Nullspace) yield break;

            // TODO: Doesn't supported nested broadphase but future sloth problem coz fuck that guy
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, aabb))
            {
                yield return EntityManager.GetEntity(grid.GridEntityId).GetComponent<BroadphaseComponent>();

                // If we're wholly in 1 grid don't need to continue.
                if (grid.WorldBounds.Encloses(aabb)) yield break;
            }

            yield return _mapManager.GetMapEntity(mapId).GetComponent<BroadphaseComponent>();
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
