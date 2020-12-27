using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics.Broadphase
{
    public class DynamicTreeBroadPhase : IBroadPhase
    {
        public MapId MapId { get; set; }

        public GridId GridId { get; set; }

        // TODO: DynamicTree seems slow at updates when we have large entity counts so when we have boxstation
        // need to suss out whether chunking it might be useful.
        private B2DynamicTree<FixtureProxy> _tree = new B2DynamicTree<FixtureProxy>(capacity: 1024);

        public void UpdatePairs(BroadphaseDelegate callback)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
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
                            callback(proxy, other);
                        }

                        return true;
                    }, proxy.AABB);
                }
            }
        }

        public bool TestOverlap(DynamicTree.Proxy proxyIdA, DynamicTree.Proxy proxyIdB)
        {
            var proxyA = _tree.GetUserData(proxyIdA);
            var proxyB = _tree.GetUserData(proxyIdB);

            return proxyA.AABB.Intersects(proxyB.AABB);
        }

        public DynamicTree.Proxy AddProxy(FixtureProxy proxy)
        {
            return _tree.CreateProxy(proxy.AABB, proxy);
        }

        public void RemoveProxy(DynamicTree.Proxy proxy)
        {
            _tree.DestroyProxy(proxy);
        }

        public void MoveProxy(DynamicTree.Proxy proxy, ref Box2 aabb, Vector2 displacement)
        {
            _tree.MoveProxy(proxy, aabb, displacement);
        }

        public FixtureProxy GetProxy(DynamicTree.Proxy proxy)
        {
            return _tree.GetUserData(proxy);
        }

        public void Query(BroadPhaseQueryCallback callback, ref Box2 aabb)
        {
            _tree.Query(proxy =>
            {
                callback(_tree.GetUserData(proxy).ProxyId);
                return true;
            }, aabb);
        }

        public void RayCast(BroadPhaseRayCastCallback callback, ref CollisionRay input)
        {
            var results = new List<RayCastResults>();
            _tree.RayCast(ref results,
                (ref List<RayCastResults> state, DynamicTree.Proxy proxy, in Vector2 pos, float distance) =>
                {
                    callback(input, proxy);
                    return true;
                }, input);
        }

        public void ShiftOrigin(Vector2 newOrigin)
        {
            _tree.ShiftOrigin(newOrigin);
        }
    }
}
