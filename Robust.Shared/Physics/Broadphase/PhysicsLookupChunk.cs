using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Broadphase
{
    internal sealed class PhysicsLookupChunk
    {
        // TODO: Look at sussing out a sweep and prune implementation

        internal const byte ChunkSize = 16;

        /// <summary>
        ///     Bottom-left corner of the chunk
        /// </summary>
        internal Vector2i Origin { get; }

        private PhysicsLookupNode[,] _nodes = new PhysicsLookupNode[ChunkSize,ChunkSize];

        internal PhysicsLookupChunk(Vector2i origin)
        {
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
            return !GetProxies().Any();
        }

        public void AddProxy(FixtureProxy proxy)
        {
            foreach (var node in GetNodes(proxy.AABB))
            {
                node.AddProxy(proxy);
            }
        }

        public void RemoveProxy(FixtureProxy proxy)
        {
            foreach (var node in GetNodes(proxy.AABB))
            {
                node.RemoveProxy(proxy);
            }
        }

        public IEnumerable<FixtureProxy> GetProxies()
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var node = _nodes[x, y];
                    foreach (var shape in node.Proxies)
                    {
                        yield return shape;
                    }
                }
            }
        }

        public IEnumerable<FixtureProxy> GetPhysicsShapes(Vector2i index)
        {
            var node = _nodes[index.X - Origin.X, index.Y - Origin.Y];

            foreach (var shape in node.Proxies)
            {
                yield return shape;
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

        private List<PhysicsLookupNode> GetNodes(Box2 aabb)
        {
            var botLeft = new Vector2i((int) MathF.Floor(aabb.Bottom), (int) MathF.Floor(aabb.Left));
            var topRight = new Vector2i((int) MathF.Ceiling(aabb.Top), (int) MathF.Ceiling(aabb.Right));

            return GetNodes(botLeft, topRight);
        }

        /// <summary>
        ///     Bounded nodes
        /// </summary>
        /// <param name="bottomLeft"></param>
        /// <param name="topRight"></param>
        /// <returns></returns>
        private List<PhysicsLookupNode> GetNodes(Vector2i bottomLeft, Vector2i topRight)
        {
            var nodes = new List<PhysicsLookupNode>(1);

            var bottomLeftBound = new Vector2i(Math.Max(bottomLeft.X - Origin.X, 0), Math.Max(bottomLeft.Y - Origin.Y, 0));
            var topRightBound = new Vector2i(Math.Min(topRight.X - Origin.X, ChunkSize - 1), Math.Min(topRight.Y - Origin.Y, ChunkSize - 1));

            for (var x = bottomLeftBound.X; x <= topRightBound.X; x++)
            {
                for (var y = bottomLeftBound.Y; y <= topRightBound.Y; y++)
                {
                    nodes.Add(_nodes[x, y]);
                }
            }

            return nodes;
        }
    }
}
