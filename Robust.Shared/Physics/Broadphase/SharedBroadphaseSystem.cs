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

        private readonly Dictionary<MapId, Dictionary<GridId, Dictionary<Vector2i, PhysicsLookupChunk>>> _graph =
                     new Dictionary<MapId, Dictionary<GridId, Dictionary<Vector2i, PhysicsLookupChunk>>>();

        /// <summary>
        ///     Need to store the nodes for each entity because if the entity is deleted its transform is no longer valid.
        /// </summary>
        private readonly Dictionary<IPhysBody, HashSet<PhysicsLookupNode>> _lastKnownNodes =
                     new Dictionary<IPhysBody, HashSet<PhysicsLookupNode>>();

        // TODO: Could potentially combine this with the EntityLookups and just have each node store multiple components or w/e
        // Save muh memory.

        /// <summary>
        ///     Yields all of the entities intersecting a particular Vector2i
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="gridId"></param>
        /// <param name="gridIndices"></param>
        /// <returns></returns>
        public IEnumerable<IPhysShape> GetShapesIntersecting(MapId mapId, GridId gridId, Vector2i gridIndices)
        {
            var grids = _graph[mapId];
            var chunks = grids[gridId];

            var chunkIndices = GetChunkIndices(gridIndices);
            if (!chunks.TryGetValue(chunkIndices, out var chunk))
            {
                yield break;
            }

            foreach (var shape in chunk.GetNode(gridIndices).PhysicsShapes)
            {
                yield return shape;
            }
        }

        // TODO: Probably just snowflake grids.

        public IEnumerable<IPhysBody> GetBodiesIntersecting(
            MapId mapId,
            Box2 worldBox,
            bool approximate = true)
        {
            var checkedEntities = new HashSet<IPhysBody>();

            foreach (var node in GetNodesInRange(mapId, worldBox))
            {
                foreach (var comp in node.PhysicsComponents)
                {
                    if (checkedEntities.Contains(comp))
                        continue;

                    checkedEntities.Add(comp);

                    if (approximate || worldBox.Intersects(comp.WorldAABB))
                    {
                        yield return comp;
                    }
                }
            }
        }

        private IEnumerable<PhysicsLookupNode> GetNodesInRange(MapId mapId, Box2 worldBox)
        {
            var range = (worldBox.BottomLeft - worldBox.Center).Length;

            // This is the max in any direction that we can get a chunk (e.g. max 2 chunks away of data).
            var (maxXDiff, maxYDiff) = ((int) (range / PhysicsLookupChunk.ChunkSize) + 1, (int) (range / PhysicsLookupChunk.ChunkSize) + 1);

            foreach (var grid in MapManager.FindGridsIntersecting(mapId, worldBox))
            {
                var localCenter = grid.WorldToLocal(worldBox.Center);
                var centerTile = new Vector2i((int) Math.Floor(localCenter.X), (int) Math.Floor(localCenter.Y));
                var chunks = _graph[mapId][grid.Index];

                var bottomLeftNodeBound = new Vector2i((int) Math.Floor(centerTile.X - range), (int) Math.Floor(centerTile.Y - range));
                var topRightNodeBound = new Vector2i((int) Math.Floor(centerTile.X + range + 1), (int) Math.Floor(centerTile.Y + range + 1));

                for (var x = -maxXDiff; x <= maxXDiff; x++)
                {
                    for (var y = -maxYDiff; y <= maxYDiff; y++)
                    {
                        var chunkIndices = GetChunkIndices(new Vector2i(centerTile.X + x * PhysicsLookupChunk.ChunkSize, centerTile.Y + y * PhysicsLookupChunk.ChunkSize));

                        if (!chunks.TryGetValue(chunkIndices, out var chunk)) continue;

                        // Now we'll check if it's in range and relevant for us
                        // (e.g. if we're on the very edge of a chunk we may need more chunks).
                        foreach (var node in chunk.GetNodes(bottomLeftNodeBound, topRightNodeBound))
                        {
                            yield return node;
                        }
                    }
                }
            }
        }

        public IEnumerable<IPhysBody> GetBodiesIntersecting(MapId mapId, Vector2 position)
        {
            var grids = _graph[mapId];

            if (MapManager.TryFindGridAt(mapId, position, out var grid))
            {
                var chunkIndices = GetChunkIndices(position);
                var offsetIndices = new Vector2i((int) (Math.Floor(position.X)), (int) (Math.Floor(position.Y)));
                var node = grids[grid.Index][chunkIndices].GetNode(offsetIndices - chunkIndices);

                foreach (var comp in node.PhysicsComponents)
                {
                    yield return comp;
                }
            }
        }

        public IEnumerable<IPhysBody> GetBodiesIntersecting(GridId gridId, Vector2i index)
        {
            var mapId = MapManager.GetGrid(gridId).ParentMapId;
            var grids = _graph[mapId];

            var chunkIndices = GetChunkIndices(index);

            if (!grids[gridId].TryGetValue(chunkIndices, out var chunk))
                yield break;

            foreach (var comp in chunk.GetPhysicsComponents(index))
            {
                yield return comp;
            }
        }

        public List<Vector2i> GetIndices(IPhysBody entity)
        {
            var results = new List<Vector2i>();

            if (!_lastKnownNodes.TryGetValue(entity, out var nodes))
            {
                return results;
            }

            foreach (var node in nodes)
            {
                results.Add(node.Indices);
            }

            return results;
        }

        private PhysicsLookupChunk GetOrCreateChunk(MapId mapId, GridId gridId, Vector2i indices)
        {
            var chunkIndices = GetChunkIndices(indices);

            if (!_graph.TryGetValue(mapId, out var grids))
            {
                grids = new Dictionary<GridId, Dictionary<Vector2i, PhysicsLookupChunk>>();
                _graph[mapId] = grids;
            }

            if (!grids.TryGetValue(gridId, out var gridChunks))
            {
                gridChunks = new Dictionary<Vector2i, PhysicsLookupChunk>();
                grids[gridId] = gridChunks;
            }

            if (!gridChunks.TryGetValue(chunkIndices, out var chunk))
            {
                chunk = new PhysicsLookupChunk(mapId, gridId, chunkIndices);
                gridChunks[chunkIndices] = chunk;
            }

            return chunk;
        }

        private Vector2i GetChunkIndices(Vector2i indices)
        {
            return new Vector2i(
                (int) (Math.Floor((float) indices.X / PhysicsLookupChunk.ChunkSize) * PhysicsLookupChunk.ChunkSize),
                (int) (Math.Floor((float) indices.Y / PhysicsLookupChunk.ChunkSize) * PhysicsLookupChunk.ChunkSize));
        }

        private Vector2i GetChunkIndices(Vector2 indices)
        {
            return new Vector2i(
                (int) (Math.Floor(indices.X / PhysicsLookupChunk.ChunkSize) * PhysicsLookupChunk.ChunkSize),
                (int) (Math.Floor(indices.Y / PhysicsLookupChunk.ChunkSize) * PhysicsLookupChunk.ChunkSize));
        }

        private HashSet<PhysicsLookupNode> GetOrCreateNodes(IPhysBody physicsComponent)
        {
            if (_lastKnownNodes.TryGetValue(physicsComponent, out var nodes))
                return nodes;

            var grids = GetEntityIndices(physicsComponent);
            var results = new HashSet<PhysicsLookupNode>();
            var mapId = physicsComponent.Owner.Transform.MapID;

            foreach (var (grid, indices) in grids)
            {
                foreach (var index in indices)
                {
                    results.Add(GetOrCreateNode(mapId, grid, index));
                }
            }

            _lastKnownNodes[physicsComponent] = results;
            return results;
        }

        private HashSet<PhysicsLookupNode> GetNodes(IPhysBody physicsComponent)
        {
            var grids = GetEntityIndices(physicsComponent);
            var results = new HashSet<PhysicsLookupNode>();
            var mapId = physicsComponent.Owner.Transform.MapID;

            foreach (var (grid, indices) in grids)
            {
                foreach (var index in indices)
                {
                    results.Add(GetOrCreateNode(mapId, grid, index));
                }
            }

            return results;
        }

        /// <summary>
        ///     Return the corresponding TileLookupNode for these indices
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="gridId"></param>
        /// <param name="indices"></param>
        /// <returns></returns>
        private PhysicsLookupNode GetOrCreateNode(MapId mapId, GridId gridId, Vector2i indices)
        {
            var chunk = GetOrCreateChunk(mapId, gridId, indices);

            return chunk.GetNode(indices);
        }

        /// <summary>
        ///     Get the relevant GridId and Vector2i for this entity for lookup.
        /// </summary>
        /// <param name="physicsComponent"></param>
        /// <returns></returns>
        private Dictionary<GridId, List<Vector2i>> GetEntityIndices(IPhysBody physicsComponent)
        {
            var entityBounds = GetEntityBox(physicsComponent);
            var results = new Dictionary<GridId, List<Vector2i>>();
            var onlyOnGrid = false;

            foreach (var grid in MapManager.FindGridsIntersecting(physicsComponent.Owner.Transform.MapID, GetEntityBox(physicsComponent)))
            {
                var indices = new List<Vector2i>();

                foreach (var tile in grid.GetTilesIntersecting(entityBounds))
                {
                    indices.Add(tile.GridIndices);
                }

                results[grid.Index] = indices;

                if (grid.WorldBounds.Encloses(entityBounds))
                    onlyOnGrid = true;
            }

            if (!onlyOnGrid)
            {
                var gridlessIndices = new List<Vector2i>();
                var leftFloor = (int) Math.Floor(entityBounds.Left);
                var bottomFloor = (int) Math.Floor(entityBounds.Bottom);

                for (var x = 0; x < Math.Ceiling(entityBounds.Width); x++)
                {
                    for (var y = 0; y < Math.Ceiling(entityBounds.Height); y++)
                    {
                        gridlessIndices.Add(new Vector2i(x + leftFloor, y + bottomFloor));
                    }
                }

                results[GridId.Invalid] = gridlessIndices;
            }

            return results;
        }

        private Box2 GetEntityBox(IPhysBody physicsComponent)
        {
            return physicsComponent.WorldAABB;
        }

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
            if (message.CanCollide)
            {
                HandlePhysicsAdd(message.PhysicsComponent);
            }
            else
            {
                HandlePhysicsRemove(message.PhysicsComponent);
            }
        }

        /*
        private void HandleEntityDeleted(EntityDeletedMessage message)
        {
            HandlePhysicsRemove(message.Entity);
        }
        */

        private void HandleTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            GetOrCreateNode(eventArgs.NewTile.MapIndex, eventArgs.NewTile.GridIndex, eventArgs.NewTile.GridIndices);
        }

        private void HandleGridCreated(GridId gridId)
        {
            var mapId = MapManager.GetGrid(gridId).ParentMapId;

            if (!_graph.TryGetValue(mapId, out var grids))
            {
                grids = new Dictionary<GridId, Dictionary<Vector2i, PhysicsLookupChunk>>();
                _graph[mapId] = grids;
            }

            grids[gridId] = new Dictionary<Vector2i, PhysicsLookupChunk>();
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
