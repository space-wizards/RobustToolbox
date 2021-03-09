using System;
using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.EntityLookup
{
    public sealed class EntityLookupChunk
    {
        internal const byte ChunkSize = 4;

        /// <summary>
        ///     Parent MapId for this chunk
        /// </summary>
        internal MapId MapId { get; }

        /// <summary>
        ///     Parent GridId for this chunk
        /// </summary>
        internal GridId GridId { get; }

        /// <summary>
        ///     Bottom-left corner of the chunk
        /// </summary>
        internal Vector2i Origin { get; }

        private EntityLookupNode[,] _nodes = new EntityLookupNode[ChunkSize,ChunkSize];

        public GameTick LastModifiedTick { get; set; } = GameTick.Zero;

        public int EntityCount = 0;

        public float Bottom => Origin.Y;

        public float Left => Origin.X;

        public float Top => Origin.Y + ChunkSize;

        public float Right => Origin.X + ChunkSize;

        internal EntityLookupChunk(MapId mapId, GridId gridId, Vector2i origin)
        {
            MapId = mapId;
            GridId = gridId;
            Origin = origin;

            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    _nodes[x, y] = new EntityLookupNode(this, new Vector2i(Origin.X + x, Origin.Y + y));
                }
            }

            // TODO: REPLACE WITH A test
            DebugTools.Assert(_nodes.Length == ChunkSize * ChunkSize, $"Length is {_nodes.Length}, size is {ChunkSize * ChunkSize}");
        }

        /// <summary>
        ///     If we're a space chunk then we can ourselves when there's no more entities intersecting us.
        /// </summary>
        public bool CanDeleteChunk()
        {
            return EntityCount <= 0;
        }

        /// <summary>
        ///     Get all entities that intersect this chunk. May return duplicates from nodes.
        /// </summary>
        /// <param name="fromTick">Only retrieve entities modified after this tick</param>
        /// <param name="includeContainers">Whether to include entities within our containermanager component</param>
        /// <param name="unique">Don't return multiple entities due to node overlaps</param>
        /// <param name="excluded">Entities we shouldn't return</param>
        /// <returns></returns>
        public IReadOnlyCollection<IEntity> GetEntities(bool includeContainers=true, bool unique=true, HashSet<EntityUid>? excluded = null)
        {
            var entities = new List<IEntity>();

            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var node = _nodes[x, y];
                    foreach (var entity in node.Entities)
                    {
                        if (includeContainers)
                        {
                            foreach (var con in entity.GetContained())
                            {
                                if (con.Deleted || excluded?.Contains(con.Uid) == true) continue;
                                entities.Add(con);
                            }
                        }

                        if (entity.Deleted || excluded?.Contains(entity.Uid) == true) continue;
                        entities.Add(entity);
                    }
                }
            }

            if (unique)
                return new HashSet<IEntity>(entities);

            return entities;
        }

        public IReadOnlyCollection<IEntity> GetEntities(Vector2i index)
        {
            var node = _nodes[index.X - Origin.X, index.Y - Origin.Y];

            return node.Entities;
        }

        public IEnumerable<EntityLookupNode> GetNodes()
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    yield return _nodes[x, y];
                }
            }
        }

        /// <summary>
        ///     Bounded nodes
        /// </summary>
        /// <param name="bottomLeft"></param>
        /// <param name="topRight"></param>
        /// <returns></returns>
        public IList<EntityLookupNode> GetNodes(Vector2i bottomLeft, Vector2i topRight)
        {
            var results = new List<EntityLookupNode>();

            var bottomLeftBound = new Vector2i(Math.Max(bottomLeft.X - Origin.X, 0), Math.Max(bottomLeft.Y - Origin.Y, 0));
            var topRightBound = new Vector2i(Math.Min(topRight.X - Origin.X, ChunkSize - 1), Math.Min(topRight.Y - Origin.Y, ChunkSize - 1));

            for (var x = bottomLeftBound.X; x <= topRightBound.X; x++)
            {
                for (var y = bottomLeftBound.Y; y <= topRightBound.Y; y++)
                {
                    results.Add(_nodes[x, y]);
                }
            }

            return results;
        }

        internal EntityLookupNode GetNode(Vector2i nodeIndices)
        {
            return _nodes[nodeIndices.X - Origin.X, nodeIndices.Y - Origin.Y];
        }
    }
}
