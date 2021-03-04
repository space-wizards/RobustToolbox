using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Broadphase
{
    public abstract class SharedBroadPhaseSystem : EntitySystem
    {
        /*
         * That's right both the system implements IBroadPhase and also each grid has its own as well.
         * The reason for this is other stuff should just be able to check for broadphase with no regard
         * for the concept of grids, whereas internally this needs to worry about it.
         */

        // Anything in a container is removed from the graph and anything removed from a container is added to the graph.

        /*
         * So the Box2D derivatives just use a generic "SynchronizeFixtures" method but it's kinda obtuse so
         * I just made other methods (AddFixture, RefreshFixtures, etc.) that are clearer on what they're doing.
         */

        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Dictionary<MapId, Dictionary<GridId, IBroadPhase>> _graph = new();

        private Dictionary<IPhysBody, List<IBroadPhase>> _lastBroadPhases = new();

        /// <summary>
        ///     Given MoveEvent and RotateEvent do the same thing we won't double up on work.
        /// </summary>
        private HashSet<IEntity> _handledThisTick = new();

        private Queue<MoveEvent> _queuedMoveEvents = new();
        private Queue<RotateEvent> _queuedRotateEvent = new();
        private Queue<EntMapIdChangedMessage> _queuedMapChanges = new();
        private Queue<FixtureUpdateMessage> _queuedFixtureUpdates = new();
        private Queue<CollisionChangeMessage> _queuedCollisionChanges = new();
        private Queue<EntInsertedIntoContainerMessage> _queuedContainerInsert = new();
        private Queue<EntRemovedFromContainerMessage> _queuedContainerRemove = new();

        private IEnumerable<IBroadPhase> BroadPhases()
        {
            foreach (var (_, grids) in _graph)
            {
                foreach (var (_, broad) in grids)
                {
                    yield return broad;
                }
            }
        }

        /// <summary>
        ///     Gets the corresponding broadphase for this grid.
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="gridId"></param>
        /// <returns>null if broadphase already destroyed or none exists</returns>
        public IBroadPhase? GetBroadPhase(MapId mapId, GridId gridId)
        {
            // Might be null if the grid is being instantiated.
            if (mapId == MapId.Nullspace || !_graph[mapId].TryGetValue(gridId, out var grid))
                return null;

            return grid;
        }

        public ICollection<IBroadPhase> GetBroadPhases(MapId mapId)
        {
            return _graph[mapId].Values;
        }

        public IEnumerable<IBroadPhase> GetBroadPhases(PhysicsComponent body)
        {
            if (!_lastBroadPhases.TryGetValue(body, out var broadPhases)) return Array.Empty<IBroadPhase>();
            return broadPhases;
        }

        // Look for now I've hardcoded grids
        public IEnumerable<(IBroadPhase Broadphase, GridId GridId)> GetBroadphases(PhysicsComponent body)
        {
            if (!_lastBroadPhases.TryGetValue(body, out var broadPhases)) yield break;

            foreach (var (_, grids) in _graph)
            {
                foreach (var broad in broadPhases)
                {
                    foreach (var (gridId, broadPhase) in grids)
                    {
                        if (broad == broadPhase)
                        {
                            yield return (broadPhase, gridId);
                            break;
                        }
                    }
                }
            }
        }

        public bool TestOverlap(FixtureProxy proxyA, FixtureProxy proxyB)
        {
            // TODO: This should only ever be called on the same grid I think so maybe just assert
            var mapA = proxyA.Fixture.Body.Owner.Transform.MapID;
            var mapB = proxyB.Fixture.Body.Owner.Transform.MapID;

            if (mapA != mapB)
                return false;

            return proxyA.AABB.Intersects(proxyB.AABB);
        }

        /// <summary>
        ///     Get the percentage that 2 bodies overlap. Ignores whether collision is turned on for either body.
        /// </summary>
        /// <param name="bodyA"></param>
        /// <param name="bodyB"></param>
        /// <returns> 0 -> 1.0f based on WorldAABB overlap</returns>
        public float IntersectionPercent(PhysicsComponent bodyA, PhysicsComponent bodyB)
        {
            // TODO: Use actual shapes and not just the AABB?
            return bodyA.GetWorldAABB(_mapManager).IntersectPercentage(bodyB.GetWorldAABB(_mapManager));
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CollisionChangeMessage>(QueueCollisionChange);
            SubscribeLocalEvent<MoveEvent>(QueuePhysicsMove);
            SubscribeLocalEvent<RotateEvent>(QueuePhysicsRotate);
            SubscribeLocalEvent<EntMapIdChangedMessage>(QueueMapChange);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(QueueContainerInsertMessage);
            SubscribeLocalEvent<EntRemovedFromContainerMessage>(QueueContainerRemoveMessage);
            SubscribeLocalEvent<FixtureUpdateMessage>(QueueFixtureUpdate);
            _mapManager.OnGridCreated += HandleGridCreated;
            _mapManager.OnGridRemoved += HandleGridRemoval;
            _mapManager.MapCreated += HandleMapCreated;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            while (_queuedMoveEvents.Count > 0)
            {
                var moveEvent = _queuedMoveEvents.Dequeue();

                // Doing this seems to fuck with tp so leave off for now I guess, it's mainly to avoid the rotate duplication
                if (_handledThisTick.Contains(moveEvent.Sender)) continue;

                _handledThisTick.Add(moveEvent.Sender);

                if (moveEvent.Sender.Deleted || !moveEvent.Sender.TryGetComponent(out PhysicsComponent? physicsComponent)) continue;

                SynchronizeFixtures(physicsComponent, moveEvent.NewPosition.ToMapPos(EntityManager) - moveEvent.OldPosition.ToMapPos(EntityManager), moveEvent.WorldAABB);
            }

            while (_queuedRotateEvent.Count > 0)
            {
                var rotateEvent = _queuedRotateEvent.Dequeue();

                if (_handledThisTick.Contains(rotateEvent.Sender)) continue;

                _handledThisTick.Add(rotateEvent.Sender);

                if (rotateEvent.Sender.Deleted || !rotateEvent.Sender.TryGetComponent(out PhysicsComponent? physicsComponent))
                    return;

                SynchronizeFixtures(physicsComponent, Vector2.Zero, rotateEvent.WorldAABB);
            }

            _handledThisTick.Clear();

            // TODO: Just call ProcessEventQueue directly?
            // Manually manage queued stuff ourself given EventBus.QueueEvent happens at the same time every time
            while (_queuedMapChanges.Count > 0)
            {
                HandleMapChange(_queuedMapChanges.Dequeue());
            }

            while (_queuedContainerInsert.Count > 0)
            {
                HandleContainerInsert(_queuedContainerInsert.Dequeue());
            }

            while (_queuedContainerRemove.Count > 0)
            {
                HandleContainerRemove(_queuedContainerRemove.Dequeue());
            }

            while (_queuedCollisionChanges.Count > 0)
            {
                var message = _queuedCollisionChanges.Dequeue();
                if (message.CanCollide && !message.Body.Deleted)
                {
                    AddBody(message.Body);
                }
                else
                {
                    RemoveBody(message.Body);
                }
            }

            while (_queuedFixtureUpdates.Count > 0)
            {
                var message = _queuedFixtureUpdates.Dequeue();
                RefreshFixture(message.Body, message.Fixture);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            UnsubscribeLocalEvent<CollisionChangeMessage>();
            UnsubscribeLocalEvent<MoveEvent>();
            UnsubscribeLocalEvent<RotateEvent>();
            UnsubscribeLocalEvent<EntMapIdChangedMessage>();
            UnsubscribeLocalEvent<EntInsertedIntoContainerMessage>();
            UnsubscribeLocalEvent<EntRemovedFromContainerMessage>();
            UnsubscribeLocalEvent<FixtureUpdateMessage>();
            _mapManager.OnGridCreated -= HandleGridCreated;
            _mapManager.OnGridRemoved -= HandleGridRemoval;
            _mapManager.MapCreated -= HandleMapCreated;
        }

        private void QueuePhysicsMove(MoveEvent moveEvent)
        {
            _queuedMoveEvents.Enqueue(moveEvent);
        }

        private void QueuePhysicsRotate(RotateEvent rotateEvent)
        {
            if (!rotateEvent.Sender.TryGetComponent(out PhysicsComponent? physicsComponent))
                return;

            SynchronizeFixtures(physicsComponent, Vector2.Zero);
        }

        private void QueueCollisionChange(CollisionChangeMessage message)
        {
            _queuedCollisionChanges.Enqueue(message);
        }

        private void QueueMapChange(EntMapIdChangedMessage message)
        {
            _queuedMapChanges.Enqueue(message);
        }

        private void QueueContainerInsertMessage(EntInsertedIntoContainerMessage message)
        {
            _queuedContainerInsert.Enqueue(message);
        }

        private void QueueContainerRemoveMessage(EntRemovedFromContainerMessage message)
        {
            _queuedContainerRemove.Enqueue(message);
        }

        private void HandleContainerInsert(EntInsertedIntoContainerMessage message)
        {
            if (!message.Entity.Deleted && message.Entity.TryGetComponent(out IPhysBody? physicsComponent))
            {
                physicsComponent.CanCollide = false;
                physicsComponent.Awake = false;
            }
        }

        private void HandleContainerRemove(EntRemovedFromContainerMessage message)
        {
            if (!message.Entity.Deleted && message.Entity.TryGetComponent(out IPhysBody? physicsComponent))
            {
                physicsComponent.CanCollide = true;
                physicsComponent.Awake = true;
            }
        }

        private void QueueFixtureUpdate(FixtureUpdateMessage message)
        {
            _queuedFixtureUpdates.Enqueue(message);
        }

        public void AddBroadPhase(PhysicsComponent body, IBroadPhase broadPhase)
        {
            if (!_lastBroadPhases.TryGetValue(body, out var broadPhases))
            {
                return;
            }

            if (broadPhases.Contains(broadPhase)) return;
            broadPhases.Add(broadPhase);
        }

        /// <summary>
        ///     Handles map changes for bodies completely
        /// </summary>
        /// <param name="message"></param>
        private void HandleMapChange(EntMapIdChangedMessage message)
        {
            if (message.Entity.Deleted ||
                !message.Entity.TryGetComponent(out PhysicsComponent? body) ||
                !_lastBroadPhases.TryGetValue(body, out var broadPhases))
            {
                return;
            }

            if (Get<SharedPhysicsSystem>().Maps.TryGetValue(message.OldMapId, out var oldMap))
            {
                oldMap.RemoveBody(body);
            }

            body.ClearProxies();

            if (Get<SharedPhysicsSystem>().Maps.TryGetValue(message.Entity.Transform.MapID, out var newMap))
            {
                newMap.AddBody(body);
                body.CreateProxies();
                SetBroadPhases(body);
            }
        }

        private void SetBroadPhases(IPhysBody body)
        {
            if (!_lastBroadPhases.TryGetValue(body, out var broadPhases))
            {
                broadPhases = new List<IBroadPhase>();
                _lastBroadPhases[body] = broadPhases;
            }

            broadPhases.Clear();

            foreach (var fixture in body.Fixtures)
            {
                foreach (var (gridId, _) in fixture.Proxies)
                {
                    var broadPhase = GetBroadPhase(body.Owner.Transform.MapID, gridId);
                    if (broadPhase == null) continue;
                    broadPhases.Add(broadPhase);
                }
            }
        }

        private void HandleGridCreated(GridId gridId)
        {
            var mapId = _mapManager.GetGrid(gridId).ParentMapId;

            if (!_graph.TryGetValue(mapId, out var grids))
            {
                grids = new Dictionary<GridId, IBroadPhase>();
                _graph[mapId] = grids;
            }

            grids[gridId] = new DynamicTreeBroadPhase(mapId, gridId);
        }

        private void HandleMapCreated(object? sender, MapEventArgs eventArgs)
        {
            _graph[eventArgs.Map] = new Dictionary<GridId, IBroadPhase>()
            {
                {
                    GridId.Invalid,
                    new DynamicTreeBroadPhase(eventArgs.Map, GridId.Invalid)
                }
            };

        }

        private void HandleGridRemoval(GridId gridId)
        {
            foreach (var (_, grids) in _graph)
            {
                if (!grids.TryGetValue(gridId, out var broadPhase)) continue;

                var toCleanup = new List<IPhysBody>();
                // Need to cleanup every body that was touching this grid.
                foreach (var (body, broadPhases) in _lastBroadPhases)
                {
                    if (broadPhases.Contains(broadPhase))
                    {
                        toCleanup.Add(body);
                    }
                }

                foreach (var body in toCleanup)
                {
                    RemoveBody(body);
                }

                grids.Remove(gridId);

                foreach (var body in toCleanup)
                {
                    AddBody(body);
                }
            }
        }

        public void AddBody(IPhysBody body)
        {
            if (_lastBroadPhases.ContainsKey(body))
            {
                return;
            }

            var mapId = body.Owner.Transform.MapID;

            if (mapId == MapId.Nullspace)
            {
                _lastBroadPhases[body] = new List<IBroadPhase>();
                return;
            }

            foreach (var fixture in body.Fixtures)
            {
                DebugTools.Assert(fixture.Proxies.Count == 0, "Can't add a body to broadphase when it already has proxies!");
            }

            var broadPhases = new List<IBroadPhase>();
            _lastBroadPhases[body] = broadPhases;

            body.CreateProxies(_mapManager, this);
            SetBroadPhases(body);
        }

        public void RemoveBody(IPhysBody body)
        {
            if (!_lastBroadPhases.ContainsKey(body))
            {
                return;
            }

            body.ClearProxies();

            _lastBroadPhases.Remove(body);
        }

        /// <summary>
        ///     Recreates this fixture in the relevant broadphases.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="fixture"></param>
        public void RefreshFixture(PhysicsComponent body, Fixture fixture)
        {
            if (!_lastBroadPhases.TryGetValue(body, out var broadPhases))
            {
                return;
            }

            var mapId = body.Owner.Transform.MapID;

            if (mapId == MapId.Nullspace || body.Deleted)
            {
                return;
            }

            fixture.ClearProxies(mapId, this);
            fixture.CreateProxies(_mapManager, this);

            // Need to update what broadphases are relevant.
            SetBroadPhases(body);
        }

        internal void AddFixture(PhysicsComponent body, Fixture fixture)
        {
            // If the entity's still being initialized it might have MoveEvent called (might change in future?)
            if (!_lastBroadPhases.TryGetValue(body, out var broadPhases))
            {
                return;
            }

            var mapId = body.Owner.Transform.MapID;
            DebugTools.Assert(fixture.Proxies.Count == 0);

            if (mapId == MapId.Nullspace || body.Deleted)
            {
                body.ClearProxies();
                return;
            }

            broadPhases.Clear();
            fixture.CreateProxies(_mapManager, this);

            foreach (var fix in body.Fixtures)
            {
                foreach (var (gridId, _) in fix.Proxies)
                {
                    var broadPhase = GetBroadPhase(mapId, gridId);
                    if (broadPhase == null) continue;
                    broadPhases.Add(broadPhase);
                }
            }
        }

        internal void RemoveFixture(PhysicsComponent body, Fixture fixture)
        {
            // If the entity's still being initialized it might have MoveEvent called (might change in future?)
            if (!_lastBroadPhases.TryGetValue(body, out var broadPhases))
            {
                return;
            }

            var mapId = body.Owner.Transform.MapID;
            fixture.ClearProxies(mapId, this);

            if (mapId == MapId.Nullspace || body.Deleted)
            {
                body.ClearProxies();
                return;
            }

            // Need to re-build the broadphases.
            broadPhases.Clear();

            foreach (var fix in body.Fixtures)
            {
                foreach (var (gridId, _) in fix.Proxies)
                {
                    var broadPhase = GetBroadPhase(mapId, gridId);
                    if (broadPhase == null) continue;
                    broadPhases.Add(broadPhase);
                }
            }
        }

        /// <summary>
        ///     Move all of the fixtures on this body.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="displacement"></param>
        private void SynchronizeFixtures(PhysicsComponent body, Vector2 displacement, Box2? worldAABB = null)
        {
            // If the entity's still being initialized it might have MoveEvent called (might change in future?)
            if (!_lastBroadPhases.TryGetValue(body, out var oldBroadPhases))
            {
                return;
            }

            var mapId = body.Owner.Transform.MapID;
            worldAABB ??= body.GetWorldAABB(_mapManager);

            // 99% of the time this is going to be 1, maybe 2, so HashSet probably slower?

            var newBroadPhases = _mapManager
                .FindGridIdsIntersecting(mapId, worldAABB.Value, true)
                .Select(gridId => GetBroadPhase(mapId, gridId))
                .ToList();

            // Remove from old broadphases
            for (var i = oldBroadPhases.Count - 1; i >= 0; i--)
            {
                var broadPhase = oldBroadPhases[i];

                if (newBroadPhases.Contains(broadPhase)) continue;

                var gridId = GetGridId(broadPhase);

                foreach (var fixture in body.Fixtures)
                {
                    fixture.ClearProxies(mapId, this, gridId);
                }

                oldBroadPhases.RemoveAt(i);
            }

            // Update retained broadphases
            // TODO: These will need swept broadPhases
            var offset = body.Owner.Transform.WorldPosition;
            var worldRotation = body.Owner.Transform.WorldRotation;

            foreach (var broadPhase in oldBroadPhases)
            {
                if (!newBroadPhases.Contains(broadPhase)) continue;

                var gridId = GetGridId(broadPhase);

                foreach (var fixture in body.Fixtures)
                {
                    if (!fixture.Proxies.TryGetValue(gridId, out var proxies)) continue;

                    foreach (var proxy in proxies)
                    {
                        double gridRotation = worldRotation;

                        if (gridId != GridId.Invalid)
                        {
                            var grid = _mapManager.GetGrid(gridId);
                            offset -= grid.WorldPosition;
                            // TODO: Should probably have a helper for this
                            gridRotation = worldRotation - body.Owner.EntityManager.GetEntity(grid.GridEntityId).Transform.WorldRotation;
                        }

                        var aabb = fixture.Shape.CalculateLocalBounds(gridRotation).Translated(offset);
                        proxy.AABB = aabb;

                        broadPhase.MoveProxy(proxy.ProxyId, in aabb, displacement);
                    }
                }
            }

            // Add to new broadphases
            foreach (var broadPhase in newBroadPhases)
            {
                if (broadPhase == null || oldBroadPhases.Contains(broadPhase)) continue;
                var gridId = GetGridId(broadPhase);
                oldBroadPhases.Add(broadPhase);

                foreach (var fixture in body.Fixtures)
                {
                    DebugTools.Assert(!fixture.Proxies.ContainsKey(gridId));

                    fixture.CreateProxies(broadPhase, _mapManager, this);
                }
            }
        }

        public GridId GetGridId(IBroadPhase broadPhase)
        {
            foreach (var (_, grids) in _graph)
            {
                foreach (var (gridId, broad) in grids)
                {
                    if (broadPhase == broad)
                    {
                        return gridId;
                    }
                }
            }

            throw new InvalidOperationException("Unable to find GridId for broadPhase");
        }

        // This is dirty but so is a lot of other shit so it'll get refactored at some stage tm
        public List<PhysicsComponent> GetAwakeBodies(MapId mapId, GridId gridId)
        {
            var bodies = new List<PhysicsComponent>();
            var map = Get<SharedPhysicsSystem>().Maps[mapId];

            foreach (var body in map.AwakeBodies)
            {
                if (body.Owner.Transform.GridID == gridId)
                    bodies.Add(body);
            }

            return bodies;
        }

        #region Queries

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

            foreach (var gridId in _mapManager.FindGridIdsIntersecting(mapId, collider, true))
            {
                var gridCollider = _mapManager.GetGrid(gridId).WorldToLocal(collider.Center);
                var gridBox = collider.Translated(gridCollider);

                _graph[mapId][gridId].QueryAabb(ref state, (ref (Box2 collider, MapId map, bool found) state, in FixtureProxy proxy) =>
                {
                    if (proxy.Fixture.CollisionLayer == 0x0)
                        return true;

                    if (proxy.AABB.Intersects(gridBox))
                    {
                        state.found = true;
                        return false;
                    }
                    return true;
                }, gridBox, approximate);
            }

            return state.found;
        }

        public IEnumerable<PhysicsComponent> GetCollidingEntities(PhysicsComponent body, Vector2 offset, bool approximate = true)
        {
            // If the body has just had its collision enabled or disabled it may not be ready yet so we'll wait a tick.
            if (!body.CanCollide || body.Owner.Transform.MapID == MapId.Nullspace)
            {
                return Array.Empty<PhysicsComponent>();
            }

            // Unfortunately due to the way grids are currently created we have to queue CanCollide event changes, hence we need to do this here.
            if (!_lastBroadPhases.ContainsKey(body))
            {
                AddBody(body);
            }

            var modifiers = body.Entity.GetAllComponents<ICollideSpecial>();
            var entities = new List<PhysicsComponent>();

            var state = (body, modifiers, entities);

            foreach (var broadPhase in _lastBroadPhases[body])
            {
                foreach (var fixture in body.Fixtures)
                {
                    foreach (var proxy in fixture.Proxies[GetGridId(broadPhase)])
                    {
                        broadPhase.QueryAabb(ref state,
                            (ref (PhysicsComponent body, IEnumerable<ICollideSpecial> modifiers, List<PhysicsComponent> entities) state,
                                in FixtureProxy other) =>
                            {
                                if (other.Fixture.Body.Deleted || other.Fixture.Body == body) return true;
                                if ((proxy.Fixture.CollisionMask & other.Fixture.CollisionLayer) == 0x0) return true;

                                var preventCollision = false;
                                var otherModifiers = other.Fixture.Body.Owner.GetAllComponents<ICollideSpecial>();

                                foreach (var modifier in state.modifiers)
                                {
                                    preventCollision |= modifier.PreventCollide(other.Fixture.Body);
                                }
                                foreach (var modifier in otherModifiers)
                                {
                                    preventCollision |= modifier.PreventCollide(body);
                                }

                                if (preventCollision)
                                    return true;

                                state.entities.Add(other.Fixture.Body);
                                return true;
                            }, proxy.AABB, approximate);
                    }
                }
            }

            return entities;
        }

        /// <summary>
        /// Get all entities colliding with a certain body.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public IEnumerable<PhysicsComponent> GetCollidingEntities(MapId mapId, in Box2 worldAABB)
        {
            if (mapId == MapId.Nullspace) return Array.Empty<PhysicsComponent>();

            var bodies = new HashSet<PhysicsComponent>();

            foreach (var gridId in _mapManager.FindGridIdsIntersecting(mapId, worldAABB, true))
            {
                Vector2 offset;

                if (gridId == GridId.Invalid)
                {
                    offset = Vector2.Zero;
                }
                else
                {
                    offset = -_mapManager.GetGrid(gridId).WorldPosition;
                }

                var gridBox = worldAABB.Translated(offset);
                foreach (var proxy in _graph[mapId][gridId].QueryAabb(gridBox, false))
                {
                    if (bodies.Contains(proxy.Fixture.Body)) continue;
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

            foreach (var gridId in _mapManager.FindGridIdsIntersecting(mapId, rayBox, true))
            {
                Vector2 offset;

                if (gridId == GridId.Invalid)
                {
                    offset = Vector2.Zero;
                }
                else
                {
                    offset = _mapManager.GetGrid(gridId).WorldPosition;
                }

                var broadPhase = GetBroadPhase(mapId, gridId);
                var gridRay = new CollisionRay(ray.Position - offset, ray.Direction, ray.CollisionMask);
                // TODO: Probably need rotation when we get rotatable grids

                broadPhase?.QueryRay((in FixtureProxy proxy, in Vector2 point, float distFromOrigin) =>
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

                    if (predicate?.Invoke(proxy.Fixture.Body.Entity) == true)
                    {
                        return true;
                    }

                    // TODO: Shape raycast here

                    // Need to convert it back to world-space.
                    var result = new RayCastResults(distFromOrigin, point + offset, proxy.Fixture.Body.Entity);
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
            // TODO: Just make an actual box
            var rayBox = new Box2(ray.Position - maxLength, ray.Position + maxLength);

            foreach (var gridId in _mapManager.FindGridIdsIntersecting(mapId, rayBox, true))
            {
                var offset = gridId == GridId.Invalid ? Vector2.Zero : _mapManager.GetGrid(gridId).WorldPosition;

                var broadPhase = GetBroadPhase(mapId, gridId);
                if (broadPhase == null) continue;

                var gridRay = new CollisionRay(ray.Position - offset, ray.Direction, ray.CollisionMask);
                // TODO: Probably need rotation when we get rotatable grids

                broadPhase.QueryRay((in FixtureProxy proxy, in Vector2 point, float distFromOrigin) =>
                {
                    if (distFromOrigin > maxLength || proxy.Fixture.Body.Entity == ignoredEnt) return true;

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
    }
}
