using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics.Broadphase
{
    internal interface IBroadPhaseManager
    {
        void AddBody(PhysicsComponent component);

        void RemoveBody(PhysicsComponent component);

        void SynchronizeFixtures(PhysicsComponent component, PhysicsTransform xf1, PhysicsTransform xf2);

        void AddProxy(FixtureProxy proxy);

        void TouchProxy(FixtureProxy proxy);

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
            // TODO: Hacky af. Maybe store the GridId on the proxy.
            foreach (var broad in BroadPhases())
            {
                if (broad.Contains(proxyA) && broad.Contains(proxyB))
                {
                    return proxyA.AABB.Intersects(proxyB.AABB);
                }
            }

            return false;
        }

        public void UpdatePairs(BroadphaseDelegate callback)
        {
            // TODO: This thing
            throw new NotImplementedException();
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

        private void HandleGridRemoval(GridId gridId)
        {
            // TODO: Get relevant map and remove the grid from it, then also shutdown the broadphase
            // Migrate all entities to their new home or some shit maybe idk, depends on order
            _mapManager.GetM
            _graph[]
        }

        /// <summary>
        ///     Tries to add the entity to the relevant TileLookupNode
        /// </summary>
        /// The node will filter it to the correct category (if possible)
        /// <param name="physicsComponent"></param>
        private void HandlePhysicsAdd(IPhysBody physicsComponent)
        {
            // TODO: I DON'T THINK TRANSFORM IS CORRECT FOR CONTAINED ENTITIES
            //PROBABLY CALL THIS WHEN AN ENTITY'S PARENT IS CHANGED
            if (physicsComponent.Deleted ||
                physicsComponent.Owner.Transform.MapID == MapId.Nullspace)
            {
                return;
            }

            // TODO: We still need grids to show up.... riiiggghhhttt?
            // Might need to look at parents I guess...? Or maybe have a separate grid collision thing?
            // TODO: Also should look if they have the physics grid shape...
            if (physicsComponent.Owner.TryGetComponent(out IMapGridComponent? mapGridComponent))
            {
                return;
            }

            var entityNodes = GetOrCreateNodes(physicsComponent);
            var newIndices = new Dictionary<GridId, List<Vector2i>>();

            foreach (var node in entityNodes)
            {
                node.AddPhysics(physicsComponent);
                if (!newIndices.TryGetValue(node.ParentChunk.GridId, out var existing))
                {
                    existing = new List<Vector2i>();
                    newIndices[node.ParentChunk.GridId] = existing;
                }

                existing.Add(node.Indices);
            }

            _lastKnownNodes[physicsComponent] = entityNodes;
            //EntityManager.EventBus.RaiseEvent(EventSource.Local, new TileLookupUpdateMessage(newIndices));
        }

        /// <summary>
        ///     Removes this entity from all of the applicable nodes.
        /// </summary>
        /// <param name="entity"></param>
        private void HandlePhysicsRemove(IPhysBody entity)
        {
            var toDelete = new List<PhysicsLookupChunk>();
            var checkedChunks = new HashSet<PhysicsLookupChunk>();

            if (_lastKnownNodes.TryGetValue(entity, out var nodes))
            {
                foreach (var node in nodes)
                {
                    if (!checkedChunks.Contains(node.ParentChunk))
                    {
                        checkedChunks.Add(node.ParentChunk);
                        if (node.ParentChunk.CanDeleteChunk())
                        {
                            toDelete.Add(node.ParentChunk);
                        }
                    }

                    node.RemovePhysics(entity);
                }
            }

            _lastKnownNodes.Remove(entity);

            foreach (var chunk in toDelete)
            {
                _graph[chunk.MapId][chunk.GridId].Remove(chunk.Origin);
            }

            //EntityManager.EventBus.RaiseEvent(EventSource.Local, new TileLookupUpdateMessage(null));
        }

        /// <summary>
        ///     When an entity moves around we'll remove it from its old node and add it to its new node (if applicable)
        /// </summary>
        /// <param name="moveEvent"></param>
        private void HandlePhysicsMove(MoveEvent moveEvent)
        {
            if (!moveEvent.Sender.TryGetComponent(out IPhysBody? physicsComponent))
                return;

            if (moveEvent.Sender.Deleted ||
                !moveEvent.NewPosition.IsValid(EntityManager))
            {
                // TODO: Need an event when a body is removed
                HandlePhysicsRemove(physicsComponent);
                return;
            }

            // This probably means it's a grid
            // TODO: REALLY NEED TO HANDLE IT BUDDY
            if (!_lastKnownNodes.TryGetValue(physicsComponent, out var oldNodes))
                return;

            // TODO: Need to add entity parenting to transform (when _localPosition is set then check its parent
            var newNodes = GetNodes(physicsComponent);
            if (oldNodes.Count == newNodes.Count && oldNodes.SetEquals(newNodes))
            {
                return;
            }

            var toRemove = oldNodes.Where(oldNode => !newNodes.Contains(oldNode));
            var toAdd = newNodes.Where(newNode => !oldNodes.Contains(newNode));

            foreach (var node in toRemove)
            {
                node.RemovePhysics(physicsComponent);
            }

            foreach (var node in toAdd)
            {
                node.AddPhysics(physicsComponent);
            }

            var newIndices = new Dictionary<GridId, List<Vector2i>>();
            foreach (var node in newNodes)
            {
                if (!newIndices.TryGetValue(node.ParentChunk.GridId, out var existing))
                {
                    existing = new List<Vector2i>();
                    newIndices[node.ParentChunk.GridId] = existing;
                }

                existing.Add(node.Indices);
            }

            _lastKnownNodes[physicsComponent] = newNodes;
        }

        public void AddBody(PhysicsComponent component)
        {
            var mapId = component.Owner.Transform.MapID;
            var grids = _graph[mapId];
            var fixtures = component.FixtureList;
            var transform = component.GetTransform();
            _lastBroadPhases[component] = new List<IBroadPhase>();

            foreach (var fixture in fixtures)
            {
                fixture.CreateProxies(transform);
            }

            // Can't use CreateProxies as we need the proxy from every fixture to be added.
            var proxies = component.GetProxies();

            foreach (var gridId in _mapManager.FindGridIdsIntersecting(mapId,
                component.WorldAABB, true))
            {
                var broadPhase = grids[gridId];

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

        public void AddProxy(FixtureProxy proxy)
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
    }
}
