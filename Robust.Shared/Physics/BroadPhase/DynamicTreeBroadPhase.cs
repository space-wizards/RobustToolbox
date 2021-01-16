using System.Collections.Generic;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Broadphase
{
    public class DynamicTreeBroadPhase : IBroadPhase
    {
        public MapId MapId { get; set; }

        public GridId GridId { get; set; }

        // TODO: DynamicTree seems slow at updates when we have large entity counts so when we have boxstation
        // need to suss out whether chunking it might be useful.
        private B2DynamicTree<FixtureProxy> _tree = new(capacity: 256);

        private readonly DynamicTree<FixtureProxy>.ExtractAabbDelegate _extractAabb = ExtractAabbFunc;
        private static readonly DynamicTree<FixtureProxy>.QueryCallbackDelegate<DynamicTree<FixtureProxy>.QueryCallbackDelegate> EasyQueryCallback =
            (ref DynamicTree<FixtureProxy>.QueryCallbackDelegate s, in FixtureProxy v) => s(v);

        public DynamicTreeBroadPhase(MapId mapId, GridId gridId)
        {
            MapId = mapId;
            GridId = gridId;
        }

        private static Box2 ExtractAabbFunc(in FixtureProxy proxy)
        {
            return proxy.AABB;
        }

        /*
        public void UpdatePairs(BroadphaseDelegate callback)
        {
            // TODO: Only check for movers rather than awake bodies potentially (tl;dr elsewhere I outlined some thoughts
            // on handling shuttles given something can be moving in worldspace but still asleep)

            foreach (var body in EntitySystem.Get<SharedBroadPhaseSystem>().GetAwakeBodies(MapId, GridId))
            {
                foreach (var proxy in body.GetProxies(GridId))
                {
                    _tree.Query(prox =>
                    {
                        var other = _tree.GetUserData(prox);
                        if (proxy.Fixture.Body.ShouldCollide(other.Fixture.Body))
                        {
                            callback(GridId, proxy, other);
                        }

                        return true;
                    }, proxy.AABB);
                }
            }
        }
        */

        public bool TestOverlap(DynamicTree.Proxy proxyIdA, DynamicTree.Proxy proxyIdB)
        {
            var proxyA = _tree.GetUserData(proxyIdA);
            var proxyB = _tree.GetUserData(proxyIdB);
            if (proxyA.Fixture.Body.Owner.Transform.GridID != proxyB.Fixture.Body.Owner.Transform.GridID) return false;

            return proxyA.AABB.Intersects(proxyB.AABB);
        }

        public DynamicTree.Proxy AddProxy(ref FixtureProxy proxy)
        {
            return _tree.CreateProxy(proxy.AABB, proxy);
        }

        public void RemoveProxy(DynamicTree.Proxy proxy)
        {
            _tree.DestroyProxy(proxy);
        }

        public void QueryAABB(DynamicTree<FixtureProxy>.QueryCallbackDelegate callback, Box2 aabb, bool approx = false)
        {
            QueryAabb(ref callback, EasyQueryCallback, aabb, approx);
        }

        public void MoveProxy(DynamicTree.Proxy proxy, ref Box2 aabb, Vector2 displacement)
        {
            _tree.MoveProxy(proxy, aabb, displacement);
        }

        public FixtureProxy GetProxy(DynamicTree.Proxy proxy)
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

        public void QueryRay(DynamicTree<FixtureProxy>.RayQueryCallbackDelegate callback, in Ray ray, bool approx = false)
        {
            QueryRay(ref callback, RayQueryDelegateCallbackInst, ray, approx);
        }

        private static readonly DynamicTree<FixtureProxy>.RayQueryCallbackDelegate<DynamicTree<FixtureProxy>.RayQueryCallbackDelegate> RayQueryDelegateCallbackInst = RayQueryDelegateCallback;

        private static bool RayQueryDelegateCallback(ref DynamicTree<FixtureProxy>.RayQueryCallbackDelegate state, in FixtureProxy value, in Vector2 point, float distFromOrigin)
        {
            return state(value, point, distFromOrigin);
        }

        public void QueryRay<TState>(ref TState state, DynamicTree<FixtureProxy>.RayQueryCallbackDelegate<TState> callback, in Ray ray, bool approx = false)
        {
            var tuple = (state, callback, _tree, approx ? null : _extractAabb, ray);
            _tree.RayCast(ref tuple, DelegateCache<TState>.RayQueryState, ray);
            state = tuple.state;
        }

        public void ShiftOrigin(Vector2 newOrigin)
        {
            _tree.ShiftOrigin(newOrigin);
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
