using System;
using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Broadphase
{
    internal sealed class PhysicsLookupChunk
    {
        internal const byte ChunkSize = 16;

        /// <summary>
        ///     Bottom-left corner of the chunk
        /// </summary>
        internal Vector2i Origin { get; }

        private PhysicsLookupNode[,] _nodes = new PhysicsLookupNode[ChunkSize,ChunkSize];

        internal PhysicsLookupChunk(Vector2i origin)
        {
            MapId = mapId;
            GridId = gridId;
            Origin = origin;

            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    _nodes[x, y] = new PhysicsLookupNode(this, new Vector2i(Origin.X + x, Origin.Y + y));
                }
            }
        }

        /// <summary>
        ///     If we're a space chunk then we can ourselves when there's no more entities intersecting us.
        /// </summary>
        public bool CanDeleteChunk()
        {
            if (GridId != GridId.Invalid)
                return false;

            foreach (var _ in GetEntities())
            {
                return false;
            }

            return true;
        }

        public IEnumerable<IPhysShape> GetShapes()
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var node = _nodes[x, y];
                    foreach (var shape in node.PhysicsShapes)
                    {
                        yield return shape;
                    }
                }
            }
        }

        public IEnumerable<IPhysShape> GetEntities()
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var node = _nodes[x, y];

                    foreach (var shape in node.PhysicsShapes)
                    {
                        yield return shape;
                    }
                }
            }
        }

        public IEnumerable<IPhysShape> GetPhysicsShapes(Vector2i index)
        {
            var node = _nodes[index.X - Origin.X, index.Y - Origin.Y];

            foreach (var shape in node.PhysicsShapes)
            {
                yield return shape;
            }
        }

        public IEnumerable<IPhysBody> GetPhysicsComponents(Vector2i index)
        {
            var node = _nodes[index.X - Origin.X, index.Y - Origin.Y];

            foreach (var comp in node.PhysicsComponents)
            {
                yield return comp;
            }
        }

        public IEnumerable<PhysicsLookupNode> GetNodes()
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
        public IEnumerable<PhysicsLookupNode> GetNodes(Vector2i bottomLeft, Vector2i topRight)
        {
            var bottomLeftBound = new Vector2i(Math.Max(bottomLeft.X - Origin.X, 0), Math.Max(bottomLeft.Y - Origin.Y, 0));
            var topRightBound = new Vector2i(Math.Min(topRight.X - Origin.X, ChunkSize - 1), Math.Min(topRight.Y - Origin.Y, ChunkSize - 1));

            for (var x = bottomLeftBound.X; x <= topRightBound.X; x++)
            {
                for (var y = bottomLeftBound.Y; y <= topRightBound.Y; y++)
                {
                    yield return _nodes[x, y];
                }
            }
        }

        internal PhysicsLookupNode GetNode(Vector2i nodeIndices)
        {
            return _nodes[nodeIndices.X - Origin.X, nodeIndices.Y - Origin.Y];
        }
    }
}
