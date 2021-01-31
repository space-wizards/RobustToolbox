using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.EntityLookup
{
    /// <summary>
    ///     Stores what entities intersect a particular tile.
    /// </summary>
    public abstract class SharedEntityLookupSystem : EntitySystem
    {
        /*
         * Look, is this the most optimised system in the world? Hell no. If you didn't have grids and optimised DynamicTree more I'd go with that.
         * As it is DynamicTree doesn't seem suited for grid-based stuff, we need something faster for grids to work.
         * Some of this is for sure snowflaked and won't support all types of transform children configs
         * (i.e. it assumes if you're a child you're still in your parent's bounds which seems reasonable???)
         *
         * This also means we don't need to keep GridTileLookupSystem around which will save a chunk of memory as
         * GetEntitiesIntersecting isn't slow af anymore.
         *
         * By all means if you come up with a better data structure that can handle grids THEN GO AHEAD.
         */

        [Dependency] protected readonly IMapManager MapManager = default!;

        private readonly Dictionary<MapId, Dictionary<GridId, Dictionary<Vector2i, EntityLookupChunk>>> _graph = new();

        /// <summary>
        ///     Need to store the nodes for each entity because if the entity is deleted its transform is no longer valid.
        /// </summary>
        protected readonly Dictionary<IEntity, HashSet<EntityLookupNode>> LastKnownNodes = new();

        public IEnumerable<IEntity> GetEntitiesInMap(MapId mapId)
        {
            foreach (var (_, grid) in _graph[mapId])
            {
                foreach (var (_, chunk) in grid)
                {
                    foreach (var entity in chunk.GetEntities())
                    {
                        yield return entity;
                    }
                }
            }
        }

        /// <summary>
        ///     Yields all of the entities intersecting a particular entity's tiles.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity)
        {
            foreach (var node in GetOrCreateNodes(entity))
            {
                foreach (var ent in node.Entities)
                {
                    yield return ent;
                }
            }
        }

        /// <summary>
        ///     Yields all of the entities intersecting a particular Vector2i
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="gridId"></param>
        /// <param name="gridIndices"></param>
        /// <returns></returns>
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, GridId gridId, Vector2i gridIndices)
        {
            var grids = _graph[mapId];
            var chunks = grids[gridId];

            var chunkIndices = GetChunkIndices(gridIndices);
            if (!chunks.TryGetValue(chunkIndices, out var chunk))
            {
                yield break;
            }

            foreach (var entity in chunk.GetNode(gridIndices).Entities)
            {
                yield return entity;
            }
        }

        public IReadOnlyList<IEntity> GetEntitiesIntersecting(
            MapId mapId,
            Box2 worldBox,
            bool includeContainers = true,
            bool includeGrids = false,
            bool includeMap = false,
            bool approximate = true)
        {
            var entities = new HashSet<IEntity>();

            if (includeMap)
            {
                entities.Add(MapManager.GetMapEntity(mapId));
            }

            if (includeGrids)
            {
                foreach (var grid in MapManager.FindGridsIntersecting(mapId, worldBox))
                {
                    var gridEntity = EntityManager.GetEntity(grid.GridEntityId);
                    entities.Add(gridEntity);
                }
            }

            foreach (var node in GetNodesInRange(mapId, worldBox))
            {
                foreach (var entity in node.Entities)
                {
                    if (!entity.Deleted && (approximate || worldBox.Intersects(EntityManager.GetWorldAabbFromEntity(entity))))
                    {
                        if (includeContainers)
                        {
                            foreach (var contained in entity.GetContained())
                            {
                                entities.Add(contained);
                            }
                        }

                        entities.Add(entity);
                    }
                }
            }

            return entities.ToList();
        }

        private IList<EntityLookupNode> GetNodesInRange(MapId mapId, Box2 worldBox)
        {
            var nodes = new List<EntityLookupNode>();
            var range = (worldBox.BottomLeft - worldBox.Center).Length;

            foreach (var chunk in GetChunksInRange(mapId, worldBox))
            {
                var localCenter = chunk.GridId == GridId.Invalid ? worldBox.Center : MapManager.GetGrid(chunk.GridId).WorldToLocal(worldBox.Center);
                var centerTile = new Vector2i((int) Math.Floor(localCenter.X), (int) Math.Floor(localCenter.Y));
                var bottomLeftNodeBound = new Vector2i((int) Math.Floor(centerTile.X - range), (int) Math.Floor(centerTile.Y - range));
                // TODO: Just use ceiling?
                var topRightNodeBound = new Vector2i((int) Math.Floor(centerTile.X + range + 1), (int) Math.Floor(centerTile.Y + range + 1));

                foreach (var node in chunk.GetNodes(bottomLeftNodeBound, topRightNodeBound))
                {
                    nodes.Add(node);
                }
            }

            return nodes;
        }

        public IList<EntityLookupChunk> GetChunksInRange(MapId mapId, Box2 worldBox, GridId? gridId=null)
        {
            var results = new List<EntityLookupChunk>();
            if (mapId == MapId.Nullspace) return results;

            var range = (worldBox.BottomLeft - worldBox.Center).Length;

            // This is the max in any direction that we can get a chunk (e.g. max 2 chunks away of data).
            var (maxXDiff, maxYDiff) = ((int) (range / EntityLookupChunk.ChunkSize) + 1, (int) (range / EntityLookupChunk.ChunkSize) + 1);

            var gridIds = gridId != null
                ? new List<GridId> {gridId.Value}
                : MapManager.FindGridIdsIntersecting(mapId, worldBox, true);

            foreach (var gid in gridIds)
            {
                var localCenter = gid == GridId.Invalid ? worldBox.Center : MapManager.GetGrid(gid).WorldToLocal(worldBox.Center);
                var localBounds = new Box2(localCenter - range, localCenter + range);
                var centerTile = new Vector2i((int) Math.Floor(localCenter.X), (int) Math.Floor(localCenter.Y));
                if (!_graph[mapId].TryGetValue(gid, out var chunks)) continue;

                for (var x = -maxXDiff; x <= maxXDiff; x++)
                {
                    for (var y = -maxYDiff; y <= maxYDiff; y++)
                    {
                        var chunkIndices = GetChunkIndices(new Vector2i(centerTile.X + x * EntityLookupChunk.ChunkSize, centerTile.Y + y * EntityLookupChunk.ChunkSize));

                        if (!chunks.TryGetValue(chunkIndices, out var chunk)) continue;

                        if (chunk.Right < localBounds.Left ||
                            chunk.Left > localBounds.Right ||
                            chunk.Bottom > localBounds.Top ||
                            chunk.Top < localBounds.Bottom) continue;

                        results.Add(chunk);
                    }
                }
            }

            return results;
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position)
        {
            var grids = _graph[mapId];

            if (MapManager.TryFindGridAt(mapId, position, out var grid))
            {
                var chunkIndices = GetChunkIndices(position);
                var offsetIndices = new Vector2i((int) (Math.Floor(position.X)), (int) (Math.Floor(position.Y)));
                var node = grids[grid.Index][chunkIndices].GetNode(offsetIndices - chunkIndices);

                foreach (var entity in node.Entities)
                {
                    yield return entity;
                }
            }
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(GridId gridId, Vector2i index)
        {
            var mapId = MapManager.GetGrid(gridId).ParentMapId;
            var grids = _graph[mapId];

            var chunkIndices = GetChunkIndices(index);

            if (!grids[gridId].TryGetValue(chunkIndices, out var chunk))
                yield break;

            foreach (var entity in chunk.GetEntities(index))
            {
                if (entity.Deleted) continue;
                yield return entity;
            }
        }

        public List<Vector2i> GetIndices(IEntity entity)
        {
            var results = new List<Vector2i>();

            if (!LastKnownNodes.TryGetValue(entity, out var nodes))
            {
                return results;
            }

            foreach (var node in nodes)
            {
                results.Add(node.Indices);
            }

            return results;
        }

        protected virtual void RemoveChunk(EntityLookupChunk chunk)
        {
            _graph[chunk.MapId][chunk.GridId].Remove(chunk.Origin);
        }

        private EntityLookupChunk GetOrCreateChunk(MapId mapId, GridId gridId, Vector2i indices)
        {
            var chunkIndices = GetChunkIndices(indices);

            if (!_graph.TryGetValue(mapId, out var grids))
            {
                grids = new Dictionary<GridId, Dictionary<Vector2i, EntityLookupChunk>>();
                _graph[mapId] = grids;
            }

            if (!grids.TryGetValue(gridId, out var gridChunks))
            {
                gridChunks = new Dictionary<Vector2i, EntityLookupChunk>();
                grids[gridId] = gridChunks;
            }

            if (!gridChunks.TryGetValue(chunkIndices, out var chunk))
            {
                chunk = new EntityLookupChunk(mapId, gridId, chunkIndices);
                gridChunks[chunkIndices] = chunk;
            }

            return chunk;
        }

        private Vector2i GetChunkIndices(Vector2i indices)
        {
            return new Vector2i(
                (int) (Math.Floor((float) indices.X / EntityLookupChunk.ChunkSize) * EntityLookupChunk.ChunkSize),
                (int) (Math.Floor((float) indices.Y / EntityLookupChunk.ChunkSize) * EntityLookupChunk.ChunkSize));
        }

        private Vector2i GetChunkIndices(Vector2 indices)
        {
            return new Vector2i(
                (int) (Math.Floor(indices.X / EntityLookupChunk.ChunkSize) * EntityLookupChunk.ChunkSize),
                (int) (Math.Floor(indices.Y / EntityLookupChunk.ChunkSize) * EntityLookupChunk.ChunkSize));
        }

        private HashSet<EntityLookupNode> GetOrCreateNodes(IEntity entity)
        {
            if (LastKnownNodes.TryGetValue(entity, out var nodes))
            {
                return nodes;
            }

            var grids = GetEntityIndices(entity);
            var results = new HashSet<EntityLookupNode>();
            var mapId = entity.Transform.MapID;

            foreach (var (grid, indices) in grids)
            {
                foreach (var index in indices)
                {
                    results.Add(GetOrCreateNode(mapId, grid, index));
                }
            }

            LastKnownNodes[entity] = results;
            return results;
        }

        /// <summary>
        ///     Gets the node for this worldposition.
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public bool TryGetNode(MapId mapId, Vector2 worldPos, [NotNullWhen(true)] out EntityLookupNode? node)
        {
            node = null;

            if (!_graph.TryGetValue(mapId, out var grids))
                return false;

            Vector2 gridPos;
            Dictionary<Vector2i, EntityLookupChunk>? chunks;

            if (MapManager.TryFindGridAt(mapId, worldPos, out var grid))
            {
                if (!grids.TryGetValue(grid.Index, out chunks))
                    return false;

                gridPos = grid.WorldToLocal(worldPos);
            }
            else
            {
                if (!grids.TryGetValue(GridId.Invalid, out chunks))
                    return false;

                gridPos = worldPos;
            }

            var chunkOrigin = GetChunkIndices(gridPos);
            if (!chunks.TryGetValue(chunkOrigin, out var chunk))
                return false;

            node = chunk.GetNode(new Vector2i((int) MathF.Floor(gridPos.X), (int) MathF.Floor(gridPos.Y)));
            return true;
        }

        public HashSet<EntityLookupNode> GetNodes(IEntity entity)
        {
            var grids = GetEntityIndices(entity);
            var results = new HashSet<EntityLookupNode>();
            var mapId = entity.Transform.MapID;

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
        private EntityLookupNode GetOrCreateNode(MapId mapId, GridId gridId, Vector2i indices)
        {
            var chunk = GetOrCreateChunk(mapId, gridId, indices);

            return chunk.GetNode(indices);
        }

        /// <summary>
        ///     Get the relevant GridId and Vector2i for this entity for lookup.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private Dictionary<GridId, List<Vector2i>> GetEntityIndices(IEntity entity)
        {
            var entityBounds = GetEntityBox(entity);
            var results = new Dictionary<GridId, List<Vector2i>>();
            var onlyOnGrid = false;

            foreach (var grid in MapManager.FindGridsIntersecting(entity.Transform.MapID, GetEntityBox(entity)))
            {
                var indices = new List<Vector2i>();

                foreach (var tile in grid.GetTilesIntersecting(entityBounds, false))
                {
                    indices.Add(tile.GridIndices);
                }

                results[grid.Index] = indices;

                if (!onlyOnGrid && grid.WorldBounds.Encloses(entityBounds))
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

        private Box2 GetEntityBox(IEntity entity)
        {
            // Need to clip the aabb as anything with an edge intersecting another tile might be picked up, such as walls.
            if (entity.TryGetComponent(out IPhysicsComponent? physics))
                return new Box2(physics.WorldAABB.BottomLeft + 0.01f, physics.WorldAABB.TopRight - 0.01f);

            // Don't want to accidentally get neighboring tiles unless we're near an edge
            return Box2.CenteredAround(entity.Transform.WorldPosition, Vector2.One / 2);
        }

        public override void Initialize()
        {
            SubscribeLocalEvent<MoveEvent>(ev => HandleEntityMove(ev.Sender));
            SubscribeLocalEvent<RotateEvent>(ev => HandleEntityMove(ev.Sender));
            SubscribeLocalEvent<EntityInitializedMessage>(HandleEntityInitialized);
            SubscribeLocalEvent<EntityDeletedMessage>(HandleEntityDeleted);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerInsert);
            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleContainerRemove);
            MapManager.OnGridCreated += HandleGridCreated;
            MapManager.OnGridRemoved += HandleGridRemoval;
            MapManager.TileChanged += HandleTileChanged;
            MapManager.MapCreated += HandleMapCreated;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            UnsubscribeLocalEvent<MoveEvent>();
            UnsubscribeLocalEvent<RotateEvent>();
            UnsubscribeLocalEvent<EntityInitializedMessage>();
            UnsubscribeLocalEvent<EntityDeletedMessage>();
            UnsubscribeLocalEvent<EntInsertedIntoContainerMessage>();
            UnsubscribeLocalEvent<EntRemovedFromContainerMessage>();
            MapManager.OnGridCreated -= HandleGridCreated;
            MapManager.OnGridRemoved -= HandleGridRemoval;
            MapManager.TileChanged -= HandleTileChanged;
            MapManager.MapCreated -= HandleMapCreated;
        }

        private void HandleEntityInitialized(EntityInitializedMessage message)
        {
            HandleEntityAdd(message.Entity);
        }

        protected virtual void HandleEntityDeleted(EntityDeletedMessage message)
        {
            HandleEntityRemove(message.Entity);
        }

        private void HandleContainerInsert(EntInsertedIntoContainerMessage message)
        {
            HandleEntityRemove(message.Entity);
        }

        private void HandleContainerRemove(EntRemovedFromContainerMessage message)
        {
            HandleEntityAdd(message.Entity);
        }

        private void HandleTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            GetOrCreateNode(eventArgs.NewTile.MapIndex, eventArgs.NewTile.GridIndex, eventArgs.NewTile.GridIndices);
        }

        private void HandleGridCreated(GridId gridId)
        {
            var mapId = MapManager.GetGrid(gridId).ParentMapId;

            if (!_graph.TryGetValue(mapId, out var grids))
            {
                grids = new Dictionary<GridId, Dictionary<Vector2i, EntityLookupChunk>>();
                _graph[mapId] = grids;
            }

            grids[gridId] = new Dictionary<Vector2i, EntityLookupChunk>();
        }

        private void HandleMapCreated(object? sender, MapEventArgs eventArgs)
        {
            _graph[eventArgs.Map] = new Dictionary<GridId, Dictionary<Vector2i, EntityLookupChunk>>();
        }

        private void HandleGridRemoval(GridId gridId)
        {
            var toRemove = new List<IEntity>();

            foreach (var (entity, _) in LastKnownNodes)
            {
                if (entity.Deleted || entity.Transform.GridID == gridId)
                    toRemove.Add(entity);
            }

            foreach (var entity in toRemove)
            {
                LastKnownNodes.Remove(entity);
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
            {
                // Cleanup player data
                foreach (var (_, chunk) in _graph[mapId.Value][gridId])
                {
                    RemoveChunk(chunk);
                }

                _graph[mapId.Value].Remove(gridId);
            }
        }

        /// <summary>
        ///     Tries to add the entity to the relevant TileLookupNode
        /// </summary>
        /// The node will filter it to the correct category (if possible)
        /// <param name="entity"></param>
        private void HandleEntityAdd(IEntity entity)
        {
            if (entity.HasComponent<MapGridComponent>() ||
                entity.Transform.Parent == null ||
                entity.IsInContainer() ||
                !entity.IsValid() ||
                entity.Transform.MapID == MapId.Nullspace ||
                LastKnownNodes.ContainsKey(entity))
            {
                return;
            }

            // TODO: Something fucky is going on with containers as powercells are shown as not in container but
            // are parented to a taser.

            foreach (var child in entity.Transform.Children)
            {
                HandleEntityAdd(child.Owner);
            }

            var entityNodes = GetOrCreateNodes(entity);
            var newIndices = new Dictionary<GridId, List<Vector2i>>();

            foreach (var node in entityNodes)
            {
                node.AddEntity(entity);
                if (!newIndices.TryGetValue(node.ParentChunk.GridId, out var existing))
                {
                    existing = new List<Vector2i>();
                    newIndices[node.ParentChunk.GridId] = existing;
                }

                existing.Add(node.Indices);
            }

            LastKnownNodes[entity] = entityNodes;
            //EntityManager.EventBus.RaiseEvent(EventSource.Local, new TileLookupUpdateMessage(newIndices));
        }

        /// <summary>
        ///     Removes this entity from all of the applicable nodes.
        /// </summary>
        /// <param name="entity"></param>
        private void HandleEntityRemove(IEntity entity)
        {
            if (!LastKnownNodes.TryGetValue(entity, out var nodes))
                return;

            var checkedChunks = new HashSet<EntityLookupChunk>();

            foreach (var node in nodes)
            {
                node.RemoveEntity(entity);
                checkedChunks.Add(node.ParentChunk);
            }

            LastKnownNodes.Remove(entity);

            foreach (var chunk in checkedChunks)
            {
                if (chunk.CanDeleteChunk())
                {
                    RemoveChunk(chunk);
                }
            }

            //EntityManager.EventBus.RaiseEvent(EventSource.Local, new TileLookupUpdateMessage(null));
        }

        /// <summary>
        ///     When an entity moves around we'll remove it from its old node and add it to its new node (if applicable)
        /// </summary>
        /// <param name="entity"></param>
        private void HandleEntityMove(IEntity entity)
        {
            if (!LastKnownNodes.TryGetValue(entity, out var oldNodes) ||
                entity.Deleted ||
                !entity.Transform.Coordinates.IsValid(EntityManager) ||
                entity.IsInContainer() ||
                entity.HasComponent<MapGridComponent>())
            {
                HandleEntityRemove(entity);
                return;
            }

            var newNodes = GetNodes(entity);
            if (oldNodes.Count == newNodes.Count && oldNodes.SetEquals(newNodes))
                return;

            var toRemove = oldNodes.Where(oldNode => !newNodes.Contains(oldNode));
            var toAdd = newNodes.Where(newNode => !oldNodes.Contains(newNode));

            foreach (var child in entity.Transform.Children)
            {
                HandleEntityMove(child.Owner);
            }

            var canDelete = new HashSet<EntityLookupChunk>();

            foreach (var node in toRemove)
            {
                node.RemoveEntity(entity);
                if (node.ParentChunk.CanDeleteChunk())
                {
                    canDelete.Add(node.ParentChunk);
                }
            }

            foreach (var node in toAdd)
            {
                node.AddEntity(entity);
                canDelete.Remove(node.ParentChunk);
            }

            foreach (var chunk in canDelete)
            {
                RemoveChunk(chunk);
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

            LastKnownNodes[entity] = newNodes;
            //EntityManager.EventBus.RaiseEvent(EventSource.Local, new TileLookupUpdateMessage(newIndices));
        }
    }
}
