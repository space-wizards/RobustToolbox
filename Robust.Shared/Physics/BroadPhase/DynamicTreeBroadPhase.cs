using System;
using System.Collections.Generic;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.Physics.Broadphase
{
    public class DynamicTreeBroadPhase : IBroadPhase
    {
        // TODO: DynamicTree seems slow at updates when we have large entity counts so when we have boxstation
        // need to suss out whether chunking it might be useful.
        private B2DynamicTree<FixtureProxy> _tree = default!;

        private readonly DynamicTree<FixtureProxy>.ExtractAabbDelegate _extractAabb = ExtractAabbFunc;

        private DynamicTree.Proxy[] _moveBuffer;
        private int _moveCapacity;
        private int _moveCount;

        private (DynamicTree.Proxy ProxyA, DynamicTree.Proxy ProxyB)[] _pairBuffer;
        private int _pairCapacity;
        private int _pairCount;
        private int _proxyCount;
        private B2DynamicTree<FixtureProxy>.QueryCallback _queryCallback;
        private DynamicTree.Proxy _queryProxyId;

        public DynamicTreeBroadPhase(int capacity)
        {
            _tree = new B2DynamicTree<FixtureProxy>(capacity);
            _queryCallback = QueryCallback;
            _proxyCount = 0;

            _pairCapacity = 16;
            _pairCount = 0;
            _pairBuffer = new (DynamicTree.Proxy ProxyA, DynamicTree.Proxy ProxyB)[_pairCapacity];

            _moveCapacity = 16;
            _moveCount = 0;
            _moveBuffer = new DynamicTree.Proxy[_moveCapacity];
        }

        public DynamicTreeBroadPhase() : this(256) {}

        private static Box2 ExtractAabbFunc(in FixtureProxy proxy)
        {
            return proxy.AABB;
        }

        public void UpdatePairs(BroadPhaseDelegate callback)
        {
            // Reset pair buffer
            _pairCount = 0;

            // Perform tree queries for all moving proxies.
            for (int j = 0; j < _moveCount; ++j)
            {
                _queryProxyId = _moveBuffer[j];
                if (_queryProxyId == DynamicTree.Proxy.Free)
                {
                    continue;
                }

                // We have to query the tree with the fat AABB so that
                // we don't fail to create a pair that may touch later.
                Box2 fatAABB;
                _tree.GetFatAABB(_queryProxyId, out fatAABB);

                // Query tree, create pairs and add them pair buffer.
                _tree.Query(_queryCallback, in fatAABB);
            }

            // Reset move buffer
            _moveCount = 0;

            // Sort the pair buffer to expose duplicates.
            Array.Sort(_pairBuffer, 0, _pairCount);

            // Send the pairs back to the client.
            int i = 0;
            while (i < _pairCount)
            {
                var primaryPair = _pairBuffer[i];
                FixtureProxy userDataA = _tree.GetUserData(primaryPair.ProxyA)!;
                FixtureProxy userDataB = _tree.GetUserData(primaryPair.ProxyB)!;

                callback(in userDataA, in userDataB);
                ++i;

                // Skip any duplicate pairs.
                while (i < _pairCount)
                {
                    (DynamicTree.Proxy ProxyA, DynamicTree.Proxy ProxyB) pair = _pairBuffer[i];
                    if (pair.ProxyA != primaryPair.ProxyA || pair.ProxyB != primaryPair.ProxyB)
                    {
                        break;
                    }
                    ++i;
                }
            }

            // Try to keep the tree balanced.
            //_tree.Rebalance(4);
        }

        /// <summary>
        /// This is called from DynamicTree.Query when we are gathering pairs.
        /// </summary>
        /// <param name="proxyId"></param>
        /// <returns></returns>
        private bool QueryCallback(DynamicTree.Proxy proxyId)
        {
            // A proxy cannot form a pair with itself.
            if (proxyId == _queryProxyId)
            {
                return true;
            }

            // Grow the pair buffer as needed.
            if (_pairCount == _pairCapacity)
            {
                (DynamicTree.Proxy ProxyA, DynamicTree.Proxy ProxyB)[] oldBuffer = _pairBuffer;
                _pairCapacity *= 2;
                _pairBuffer = new (DynamicTree.Proxy ProxyA, DynamicTree.Proxy ProxyB)[_pairCapacity];
                Array.Copy(oldBuffer, _pairBuffer, _pairCount);
            }

            _pairBuffer[_pairCount].ProxyA = new DynamicTree.Proxy(Math.Min(proxyId, _queryProxyId));
            _pairBuffer[_pairCount].ProxyB = new DynamicTree.Proxy(Math.Max(proxyId, _queryProxyId));
            _pairCount++;

            return true;
        }

        // TODO: Refactor to use fatAABB
        /// <summary>
        ///     Already assumed to be within the same broadphase.
        /// </summary>
        /// <param name="proxyIdA"></param>
        /// <param name="proxyIdB"></param>
        /// <returns></returns>
        public bool TestOverlap(DynamicTree.Proxy proxyIdA, DynamicTree.Proxy proxyIdB)
        {
            var proxyA = _tree.GetUserData(proxyIdA);
            var proxyB = _tree.GetUserData(proxyIdB);

            if (proxyA == null || proxyB == null) return false;

            return proxyB.AABB.Intersects(proxyA.AABB);
        }

        public DynamicTree.Proxy AddProxy(ref FixtureProxy proxy)
        {
            var proxyID = _tree.CreateProxy(proxy.AABB, proxy);
            _proxyCount++;
            BufferMove(proxyID);
            return proxyID;
        }

        public void MoveProxy(DynamicTree.Proxy proxy, in Box2 aabb, Vector2 displacement)
        {
            var buffer = _tree.MoveProxy(proxy, in aabb, displacement);
            if (buffer)
            {
                BufferMove(proxy);
            }
        }

        public void TouchProxy(DynamicTree.Proxy proxy)
        {
            BufferMove(proxy);
        }

        private void BufferMove(DynamicTree.Proxy proxyId)
        {
            if (_moveCount == _moveCapacity)
            {
                DynamicTree.Proxy[] oldBuffer = _moveBuffer;
                _moveCapacity *= 2;
                _moveBuffer = new DynamicTree.Proxy[_moveCapacity];
                Array.Copy(oldBuffer, _moveBuffer, _moveCount);
            }

            _moveBuffer[_moveCount] = proxyId;
            _moveCount++;
        }

        private void UnBufferMove(int proxyId)
        {
            for (int i = 0; i < _moveCount; ++i)
            {
                if (_moveBuffer[i] == proxyId)
                {
                    _moveBuffer[i] = DynamicTree.Proxy.Free;
                }
            }
        }

        public void RemoveProxy(DynamicTree.Proxy proxy)
        {
            UnBufferMove(proxy);
            _proxyCount--;
            _tree.DestroyProxy(proxy);
        }

        public void QueryAABB(DynamicTree<FixtureProxy>.QueryCallbackDelegate callback, Box2 aabb, bool approx = false)
        {
            QueryAabb(ref callback, EasyQueryCallback, aabb, approx);
        }

        public FixtureProxy? GetProxy(DynamicTree.Proxy proxy)
        {
            return _tree.GetUserData(proxy);
        }

        public void QueryAabb(DynamicTree<FixtureProxy>.QueryCallbackDelegate callback, Box2 aabb, bool approx = false)
        {
            QueryAabb(ref callback, EasyQueryCallback, aabb, approx);
        }

        public void QueryAabb<TState>(ref TState state, DynamicTree<FixtureProxy>.QueryCallbackDelegate<TState> callback, Box2 aabb, bool approx = false)
        {
            var tuple = (state, _tree, callback, aabb, approx, _extractAabb);
            _tree.Query(ref tuple, DelegateCache<TState>.AabbQueryState, aabb);
            state = tuple.state;
        }

        public IEnumerable<FixtureProxy> QueryAabb(Box2 aabb, bool approx = false)
        {
            var list = new List<FixtureProxy>();

            QueryAabb(ref list, (ref List<FixtureProxy> lst, in FixtureProxy i) =>
            {
                lst.Add(i);
                return true;
            }, aabb, approx);

            return list;
        }

        public void QueryPoint(DynamicTree<FixtureProxy>.QueryCallbackDelegate callback, Vector2 point, bool approx = false)
        {
            QueryPoint(ref callback, EasyQueryCallback, point, approx);
        }

        public void QueryPoint<TState>(ref TState state, DynamicTree<FixtureProxy>.QueryCallbackDelegate<TState> callback, Vector2 point, bool approx = false)
        {
            var tuple = (state, _tree, callback, point, approx, _extractAabb);
            _tree.Query(ref tuple,
                (ref (TState state, B2DynamicTree<FixtureProxy> tree, DynamicTree<FixtureProxy>.QueryCallbackDelegate<TState> callback, Vector2 point, bool approx, DynamicTree<FixtureProxy>.ExtractAabbDelegate extract) tuple,
                    DynamicTree.Proxy proxy) =>
                {
                    var item = tuple.tree.GetUserData(proxy)!;

                    if (!tuple.approx)
                    {
                        var precise = tuple.extract(item);
                        if (!precise.Contains(tuple.point))
                        {
                            return true;
                        }
                    }

                    return tuple.callback(ref tuple.state, item);
                }, Box2.CenteredAround(point, new Vector2(0.1f, 0.1f)));
            state = tuple.state;
        }

        public IEnumerable<FixtureProxy> QueryPoint(Vector2 point, bool approx = false)
        {
            var list = new List<FixtureProxy>();

            QueryPoint(ref list, (ref List<FixtureProxy> list, in FixtureProxy i) =>
            {
                list.Add(i);
                return true;
            }, point, approx);

            return list;
        }

        private static readonly DynamicTree<FixtureProxy>.QueryCallbackDelegate<DynamicTree<FixtureProxy>.QueryCallbackDelegate> EasyQueryCallback =
            (ref DynamicTree<FixtureProxy>.QueryCallbackDelegate s, in FixtureProxy v) => s(v);

        public void QueryRay<TState>(ref TState state, DynamicTree<FixtureProxy>.RayQueryCallbackDelegate<TState> callback, in Ray ray, bool approx = false)
        {
            var tuple = (state, callback, _tree, approx ? null : _extractAabb, ray);
            _tree.RayCast(ref tuple, DelegateCache<TState>.RayQueryState, ray);
            state = tuple.state;
        }

        private static bool AabbQueryStateCallback<TState>(ref (TState state, B2DynamicTree<FixtureProxy> tree, DynamicTree<FixtureProxy>.QueryCallbackDelegate<TState> callback, Box2 aabb, bool approx, DynamicTree<FixtureProxy>.ExtractAabbDelegate extract) tuple, DynamicTree.Proxy proxy)
        {
            var item = tuple.tree.GetUserData(proxy)!;
            if (!tuple.approx)
            {
                var precise = tuple.extract(item);
                if (!precise.Intersects(tuple.aabb))
                {
                    return true;
                }
            }

            return tuple.callback(ref tuple.state, item);
        }

        private static bool RayQueryStateCallback<TState>(ref (TState state, DynamicTree<FixtureProxy>.RayQueryCallbackDelegate<TState> callback, B2DynamicTree<FixtureProxy> tree, DynamicTree<FixtureProxy>.ExtractAabbDelegate? extract, Ray srcRay) tuple, DynamicTree.Proxy proxy, in Vector2 hitPos, float distance)
        {
            var item = tuple.tree.GetUserData(proxy)!;
            var hit = hitPos;

            if (tuple.extract != null)
            {
                var precise = tuple.extract(item);
                if (!tuple.srcRay.Intersects(precise, out distance, out hit))
                {
                    return true;
                }
            }

            return tuple.callback(ref tuple.state, item, hit, distance);
        }

        public void QueryRay(DynamicTree<FixtureProxy>.RayQueryCallbackDelegate callback, in Ray ray, bool approx = false)
        {
            QueryRay(ref callback, RayQueryDelegateCallbackInst, ray, approx);
        }

        private static readonly DynamicTree<FixtureProxy>.RayQueryCallbackDelegate<DynamicTree<FixtureProxy>.RayQueryCallbackDelegate> RayQueryDelegateCallbackInst = RayQueryDelegateCallback;

        private static bool RayQueryDelegateCallback(ref DynamicTree<FixtureProxy>.RayQueryCallbackDelegate state, in FixtureProxy value, in Vector2 point, float distFromOrigin)
        {
            return state(value, point, distFromOrigin);
        }

        private static class DelegateCache<TState>
        {
            public static readonly
                B2DynamicTree<FixtureProxy>.QueryCallback<(TState state, B2DynamicTree<FixtureProxy> tree, DynamicTree<FixtureProxy>.QueryCallbackDelegate<TState> callback, Box2 aabb, bool approx, DynamicTree<FixtureProxy>.ExtractAabbDelegate extract)> AabbQueryState =
                    AabbQueryStateCallback;

            public static readonly
                B2DynamicTree<FixtureProxy>.RayQueryCallback<(TState state, DynamicTree<FixtureProxy>.RayQueryCallbackDelegate<TState> callback,
                    B2DynamicTree<FixtureProxy> tree, DynamicTree<FixtureProxy>.ExtractAabbDelegate? extract, Ray srcRay)> RayQueryState =
                    RayQueryStateCallback;
        }
    }
}
