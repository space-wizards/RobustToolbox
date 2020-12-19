using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Broadphase
{
    internal sealed class SharedBroadphaseSystem : EntitySystem
    {
        // TODO: Have message for stuff inserted into containers
        // Anything in a container is removed from the graph and anything removed from a container is added to the graph.

        // TODO: This thing is going to memory leak like a motherfucker for space so need to handle that.
        // Ideally you'd pool space chunks.

        [Dependency] protected readonly IMapManager MapManager = default!;

        private readonly Dictionary<MapId, Dictionary<GridId, IBroadPhase>> _graph =
                     new Dictionary<MapId, Dictionary<GridId, IBroadPhase>>();

        public IEnumerable<IBroadPhase> GetBroadphases(PhysicsComponent body)
        {
            // TODO: Snowflake grids here
            var grids = _graph[body.Owner.Transform.MapID];

            foreach (var gridId in MapManager.FindGridIdsIntersecting(body.Owner.Transform.MapID, body.WorldAABB, true))
            {
                yield return grids[gridId];
            }
        }

        // TODO: Probably just snowflake grids.

        // TODO: For now I'm just using DynamicTree

        public override void Initialize()
        {
            SubscribeLocalEvent<MoveEvent>(HandlePhysicsMove);
            SubscribeLocalEvent<CollisionChangeMessage>(HandleCollisionChange);
            MapManager.OnGridCreated += HandleGridCreated;
            MapManager.OnGridRemoved += HandleGridRemoval;
            MapManager.TileChanged += HandleTileChanged;
            MapManager.MapCreated += HandleMapCreated;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            MapManager.OnGridCreated -= HandleGridCreated;
            MapManager.OnGridRemoved -= HandleGridRemoval;
            MapManager.TileChanged -= HandleTileChanged;
            MapManager.MapCreated -= HandleMapCreated;
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
            var mapId = MapManager.GetGrid(gridId).ParentMapId;

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
            _graph[eventArgs.Map] = new Dictionary<GridId, Dictionary<Vector2i, PhysicsLookupChunk>>();
        }

        private void HandleGridRemoval(GridId gridId)
        {
            var toRemove = new List<IPhysBody>();

            foreach (var (physicsComponent, _) in _lastKnownNodes)
            {
                if (physicsComponent.Deleted || physicsComponent.Owner.Transform.GridID == gridId)
                    toRemove.Add(physicsComponent);
            }

            foreach (var entity in toRemove)
            {
                _lastKnownNodes.Remove(entity);
            }

            MapId? mapId = null;

            foreach (var (map, grids) in _graph)
            {
                foreach (var (grid, _) in grids)
                {
                    if (gridId == grid)
                    {
                        mapId = map;
                        break;
                    }
                }

                if (mapId != null)
                    break;
            }

            if (mapId != null)
                _graph[mapId.Value].Remove(gridId);
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
    }
}
