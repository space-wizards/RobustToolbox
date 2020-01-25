using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    class BroadPhaseNaive : IBroadPhase
    {
        private const int NullIndex = -1;
        private const int AllocatedIndex = -2;

        private const int ArrayInitialSize = 256;
        private const int ArrayGrowthFactor = 2;

        private Node[] _nodes;
        private int _firstFreeNode;

        public BroadPhaseNaive()
            : this(ArrayInitialSize) { }

        public BroadPhaseNaive(int capacity)
        {
            if(capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than one.");

            _nodes = new Node[capacity];
            SetupFreeList(0);
        }

        private struct Node
        {
            // Next free or next allocated index.
            public int NextIndex;

            // Node data.
            public BodyProxy Proxy;
        }

        private void CheckStorageSize()
        {
            if (_firstFreeNode != NullIndex)
                return;

            var oldNodes = _nodes;
            _nodes = new Node[_nodes.Length * ArrayGrowthFactor];
            Array.Copy(oldNodes, _nodes, oldNodes.Length);

            SetupFreeList(oldNodes.Length);
        }

        private void SetupFreeList(int startingNode)
        {
            _firstFreeNode = startingNode;

            for (var i = startingNode; i < _nodes.Length - 1; i++)
                _nodes[i].NextIndex = i + 1;

            _nodes[^1].NextIndex = NullIndex;
        }

        /// <inheritdoc />
        public int AddProxy(in BodyProxy proxy)
        {
            CheckStorageSize();

            // remove a node from the free list
            var nodeId = _firstFreeNode;
            _firstFreeNode = _nodes[nodeId].NextIndex;

            // init values on the "new" node
            _nodes[nodeId].NextIndex = AllocatedIndex;
            _nodes[nodeId].Proxy = proxy;

            return nodeId;
        }

        /// <inheritdoc />
        public BodyProxy GetProxy(int proxyId)
        {
            DebugTools.Assert(_nodes[proxyId].NextIndex == AllocatedIndex);

            return _nodes[proxyId].Proxy;
        }

        public void MoveProxy(int proxyId, ref Box2 aabb, Vector2 displacement)
        {
            DebugTools.Assert(_nodes[proxyId].NextIndex == AllocatedIndex);

            // Does nothing in this implementation.
        }

        /// <inheritdoc />
        public void SetProxy(int proxyId, ref BodyProxy proxy)
        {
            DebugTools.Assert(_nodes[proxyId].NextIndex == AllocatedIndex);

            _nodes[proxyId].Proxy = proxy;
        }

        /// <inheritdoc />
        public void RemoveProxy(int proxyId)
        {
            DebugTools.Assert(_nodes[proxyId].NextIndex == AllocatedIndex);

            // insert the free'd node at the front of the freelist
            _nodes[proxyId].NextIndex = _firstFreeNode;
            _firstFreeNode = proxyId;
        }

        /// <inheritdoc />
        public void Query(QueryCallback callback, in Box2 aabb)
        {
            if(callback == null)
                throw new ArgumentNullException(nameof(callback));

            for (var i = 0; i < _nodes.Length; i++)
            {
                var node = _nodes[i];
                if (node.NextIndex == AllocatedIndex && aabb.Intersects(node.Proxy.Body.WorldAABB) && !callback(i))
                    return;
            }
        }

        /// <inheritdoc />
        public bool Test(int proxyA, int proxyB)
        {
            var a = _nodes[proxyA].Proxy.Body;
            var b = _nodes[proxyB].Proxy.Body;

            return TestBody(a, b);
        }

        private static bool TestBody(IPhysBody a, IPhysBody b)
        {
            if (a == b)
                return false;

            if (!a.CollisionEnabled || !b.CollisionEnabled)
                return false;

            if ((a.CollisionMask & b.CollisionLayer) == 0x0 &&
                (b.CollisionMask & a.CollisionLayer) == 0x0)
                return false;

            return a.MapID == b.MapID && a.WorldAABB.Intersects(b.WorldAABB);
        }

        /// <inheritdoc />
        public void RayCast(RayCastCallback callback, MapId mapId, in Ray ray, float maxLength = 25)
        {
            var minDist = maxLength;
            for (var i = 0; i < _nodes.Length; i++)
            {
                var node = _nodes[i];
                if(node.NextIndex != AllocatedIndex)
                    continue;

                var body = node.Proxy.Body;

                if(mapId != body.MapID)
                    continue;

                if(!body.CollisionEnabled)
                    continue;

                if(!body.IsHardCollidable)
                    continue;

                if ((ray.CollisionMask & body.CollisionLayer) == 0x0)
                    continue;

                if (!ray.Intersects(body.WorldAABB, out var dist, out var hitPos) || !(dist < minDist))
                    continue;

                if (!callback(i, new RayCastResults(dist, hitPos, body.Owner)))
                    continue;

                minDist = dist;
            }
        }

        /// <inheritdoc />
        public void Update(BroadPhaseCallback callback)
        {
            for (var i = 0; i < _nodes.Length; i++)
            {
                var a = _nodes[i];
                if(a.NextIndex != AllocatedIndex)
                    continue;

                for (var j = i + 1; j < _nodes.Length; j++)
                {
                    var b = _nodes[j];
                    if(b.NextIndex != AllocatedIndex)
                        continue;

                    if (TestBody(a.Proxy.Body, b.Proxy.Body))
                        callback(i, j);
                }
            }
        }
    }
}
