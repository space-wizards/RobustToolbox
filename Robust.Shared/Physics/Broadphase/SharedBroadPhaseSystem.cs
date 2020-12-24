using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Broadphase
{
    internal interface IBroadPhaseManager
    {
        // More or less a clone of IBroadPhase but this one doesn't really care about what map it's on.

        void AddBody(PhysicsComponent component);

        void RemoveBody(PhysicsComponent component);

        void SynchronizeFixtures(PhysicsComponent component, PhysicsTransform xf1, PhysicsTransform xf2);

        void DestroyProxies(Fixture fixture);

        void TouchProxy(FixtureProxy proxy);

        void UpdatePairs(MapId mapId, BroadphaseDelegate callback);

        bool TestOverlap(FixtureProxy proxyA, FixtureProxy proxyB);

        /// <summary>
        ///     Call when a fixture is added directly to a body that's already in broadphase.
        /// </summary>
        /// <param name="fixture"></param>
        void CreateProxies(Fixture fixture);

        // TODO: Query and RayCast using the old methods
    }

    internal sealed class SharedBroadPhaseSystem : EntitySystem, IBroadPhaseManager
    {
        /*
         * That's right both the system implements IBroadPhase and also each grid has its own as well.
         * The reason for this is other stuff should just be able to check for broadphase with no regard
         * for the concept of grids, whereas internally this needs to worry about it.
         */

        // TODO: Have message for stuff inserted into containers
        // Anything in a container is removed from the graph and anything removed from a container is added to the graph.

        // TODO: This thing is going to memory leak like a motherfucker for space so need to handle that.
        // Ideally you'd pool space chunks.

        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Dictionary<MapId, Dictionary<GridId, IBroadPhase>> _graph =
                     new Dictionary<MapId, Dictionary<GridId, IBroadPhase>>();

        private Dictionary<PhysicsComponent, List<IBroadPhase>> _lastBroadPhases =
            new Dictionary<PhysicsComponent, List<IBroadPhase>>(1);

        // Raycasts
        private RayCastReportFixtureDelegate? _rayCastDelegateTmp;

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

        public IBroadPhase GetBroadPhase(GridId gridId)
        {
            var mapId = _mapManager.GetGrid(gridId).ParentMapId;
            return _graph[mapId][gridId];
        }

        // Look for now I've hardcoded grids
        public IEnumerable<(IBroadPhase Broadphase, IMapGrid Grid)> GetBroadphases(PhysicsComponent body)
        {
            // TODO: Snowflake grids here
            var grids = _graph[body.Owner.Transform.MapID];

            foreach (var gridId in _mapManager.FindGridIdsIntersecting(body.Owner.Transform.MapID, body.WorldAABB, true))
            {
                yield return (grids[gridId], _mapManager.GetGrid(gridId));
            }
        }

        public bool TestOverlap(FixtureProxy proxyA, FixtureProxy proxyB)
        {
            var mapA = proxyA.Fixture.Body.Owner.Transform.MapID;
            var mapB = proxyB.Fixture.Body.Owner.Transform.MapID;

            if (mapA != mapB)
                return false;

            // TODO: Hacky af. Maybe store the GridIds on the body
            foreach (var (_, broad) in _graph[mapA])
            {
                if (broad.Contains(proxyA) && broad.Contains(proxyB))
                {
                    return proxyA.AABB.Intersects(proxyB.AABB);
                }
            }

            return false;
        }

        public void UpdatePairs(MapId mapId, BroadphaseDelegate callback)
        {
            foreach (var (_, broadPhase) in _graph[mapId])
            {
                broadPhase.UpdatePairs(callback);
            }
        }

        // TODO: Probably just snowflake grids.

        // TODO: For now I'm just using DynamicTree

        public override void Initialize()
        {
            SubscribeLocalEvent<MoveEvent>(HandlePhysicsMove);
            SubscribeLocalEvent<CollisionChangeMessage>(HandleCollisionChange);
            SubscribeLocalEvent<EntMapIdChangedMessage>(HandleMapChange);
            _mapManager.OnGridCreated += HandleGridCreated;
            _mapManager.OnGridRemoved += HandleGridRemoval;
            _mapManager.MapCreated += HandleMapCreated;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.OnGridCreated -= HandleGridCreated;
            _mapManager.OnGridRemoved -= HandleGridRemoval;
            _mapManager.MapCreated -= HandleMapCreated;
        }

        /// <summary>
        ///     Handles map changes for bodies completely
        /// </summary>
        /// <param name="message"></param>
        private void HandleMapChange(EntMapIdChangedMessage message)
        {
            if (!message.Entity.TryGetComponent(out PhysicsComponent? physicsComponent))
            {
                return;
            }

            var oldMap = Get<SharedPhysicsSystem>().Maps[message.OldMapId];
            oldMap.Remove(physicsComponent);

            var newMap = Get<SharedPhysicsSystem>().Maps[message.Entity.Transform.MapID];
            newMap.Add(physicsComponent);

            // TODO: Broadphases
            var proxies = physicsComponent.GetProxies();

            foreach (var broadPhase in _lastBroadPhases[physicsComponent])
            {
                foreach (var proxy in proxies)
                {
                    broadPhase.RemoveProxy(proxy);
                }
            }

            _lastBroadPhases[physicsComponent].Clear();

            foreach (var (broadPhase, _) in GetBroadphases(physicsComponent))
            {
                _lastBroadPhases[physicsComponent].Add(broadPhase);
                foreach (var proxy in proxies)
                {
                    broadPhase.AddProxy(proxy);
                }
            }
        }

        private void HandlePhysicsMove(MoveEvent moveEvent)
        {
            // TODO: Remove from old grids and add to new grids
            // TODO: Update existing grids
        }

        private void HandleCollisionChange(CollisionChangeMessage message)
        {
            if (message.Enabled)
            {
                HandlePhysicsAdd(message.PhysicsComponent);
            }
            else
            {
                HandlePhysicsRemove(message.PhysicsComponent);
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

            /*
             * The reason I didn't just use our existing DynamicTree was mainly because I'd need to fuck around with making
             * it use IBroadphase (for now I'm more concerned about a 1-1 port than trying to optimise it).
             * It seemed easier to just use a chunked version for now.
             */

            grids[gridId] = new ChunkBroadphase();
        }

        private void HandleMapCreated(object? sender, MapEventArgs eventArgs)
        {
            _graph[eventArgs.Map] = new Dictionary<GridId, IBroadPhase>();
        }


        /*
         * TODO: Just use b2DynamicTree again this stuff sucks, at least for now.
         */


        private void HandleGridRemoval(GridId gridId)
        {
            // TODO: Get relevant map and remove the grid from it, then also shutdown the broadphase
            // Migrate all entities to their new home or some shit maybe idk, depends on order
            _mapManager.GetM
            _graph[]
        }

        public void AddBody(PhysicsComponent component)
        {
            var mapId = component.Owner.Transform.MapID;
            var grids = _graph[mapId];
            var fixtures = component.FixtureList;
            var transform = component.GetTransform();
            _lastBroadPhases[component] = new List<IBroadPhase>();

            foreach (var gridId in _mapManager.FindGridIdsIntersecting(mapId,
                component.WorldAABB, true))
            {
                var broadPhase = grids[gridId];

                foreach (var fixture in fixtures)
                {
                    fixture.CreateProxies(gridId, transform);
                }

                var proxies = component.GetProxies(gridId);

                foreach (var proxy in proxies)
                {
                    broadPhase.AddProxy(proxy);
                }

                _lastBroadPhases[component].Add(broadPhase);
            }
        }

        public void RemoveBody(PhysicsComponent component)
        {
            var proxies = component.GetProxies();

            foreach (var broadPhase in _lastBroadPhases[component])
            {
                foreach (var proxy in proxies)
                {
                    broadPhase.RemoveProxy(proxy);
                }
            }

            _lastBroadPhases.Remove(component);
        }

        public void SynchronizeFixtures(PhysicsComponent component, PhysicsTransform xf1, PhysicsTransform xf2)
        {
            var proxies = component.GetProxies();

            // TODO: Optimise this shit, should move on the retained ones.
            foreach (var broadPhase in _lastBroadPhases[component])
            {
                for (var i = 0; i < proxies.Count; i++)
                {
                    broadPhase.RemoveProxy(proxies[i]);
                }
            }

            var mapId = component.Owner.Transform.MapID;
            var grids = _graph[mapId];
            _lastBroadPhases[component] = new List<IBroadPhase>();

            for (var i = 0; i < proxies.Count; i++)
            {
                var proxy = proxies[i];
                proxy.AABB = SynchronizeAABB(proxy, xf1, xf2);
            }

            foreach (var gridId in _mapManager.FindGridIdsIntersecting(mapId,
                component.WorldAABB, true))
            {
                var broadPhase = grids[gridId];

                for (var i = 0; i < proxies.Count; i++)
                {
                    broadPhase.AddProxy(proxies[i]);
                }

                _lastBroadPhases[component].Add(broadPhase);
            }
        }

        private Box2 SynchronizeAABB(FixtureProxy proxy, PhysicsTransform xf1, PhysicsTransform xf2)
        {
            var aabb = proxy.Fixture.Shape.ComputeAABB(xf1, proxy.ChildIndex);
            return aabb.Combine(proxy.Fixture.Shape.ComputeAABB(xf2, proxy.ChildIndex));
        }

        public void DestroyProxies(Fixture fixture)
        {
            throw new NotImplementedException();
        }

        public void TouchProxy(FixtureProxy proxy)
        {
            throw new NotImplementedException();
        }

        public void CreateProxies(Fixture fixture)
        {
            var proxies = fixture.CreateProxies(fixture.Body.GetTransform());

            foreach (var broadPhase in _lastBroadPhases[fixture.Body])
            {
                foreach (var proxy in proxies)
                {
                    broadPhase.AddProxy(proxy);
                }
            }
        }

        // This is dirty but so is a lot of other shit so it'll get refactored at some stage tm
        public IEnumerable<PhysicsComponent> GetAwakeBodies(MapId mapId, GridId gridId)
        {
            var map = Get<SharedPhysicsSystem>().Maps[mapId];

            foreach (var body in map.AwakeBodySet)
            {
                if (body.Owner.Transform.GridID == gridId)
                    yield return body;
            }
        }
    }
}
