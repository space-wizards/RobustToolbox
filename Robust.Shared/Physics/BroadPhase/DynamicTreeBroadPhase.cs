using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.Physics.Broadphase
{
    public sealed class DynamicTreeBroadPhase : IBroadPhase
    {
        private readonly B2DynamicTree<FixtureProxy> _tree;

        private readonly DynamicTree<FixtureProxy>.ExtractAabbDelegate _extractAabb = ExtractAabbFunc;

        public DynamicTreeBroadPhase(int capacity)
        {
            _tree = new B2DynamicTree<FixtureProxy>(capacity: capacity);
        }

        public DynamicTreeBroadPhase() : this(256) {}

        private static Box2 ExtractAabbFunc(in FixtureProxy proxy)
        {
            return proxy.AABB;
        }

        public Box2 GetFatAabb(DynamicTree.Proxy proxy)
        {
            return _tree.GetFatAabb(proxy);
        }

        public DynamicTree.Proxy AddProxy(ref FixtureProxy proxy)
        {
            var proxyId = _tree.CreateProxy(proxy.AABB, proxy);
            return proxyId;
        }

        public void MoveProxy(DynamicTree.Proxy proxy, in Box2 aabb, Vector2 displacement)
        {
            _tree.MoveProxy(proxy, in aabb, displacement);
        }

        public void RemoveProxy(DynamicTree.Proxy proxy)
        {
            _tree.DestroyProxy(proxy);
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
            return QueryAabb(list, aabb, approx);
        }

        public IEnumerable<FixtureProxy> QueryAabb(List<FixtureProxy> proxies, Box2 aabb, bool approx = false)
        {
            QueryAabb(ref proxies, (ref List<FixtureProxy> lst, in FixtureProxy i) =>
            {
                lst.Add(i);
                return true;
            }, aabb, approx);

            return proxies;
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
