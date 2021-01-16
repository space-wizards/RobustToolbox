using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public struct FixtureProxy
    {
        /// <summary>
        ///     Grid-based AABB of this proxy.
        /// </summary>
        public Box2 AABB;

        /// <summary>
        ///     Our parent fixture
        /// </summary>
        public Fixture Fixture;

        /// <summary>
        ///     ID of this proxy in the broadphase dynamictree.
        /// </summary>
        public DynamicTree.Proxy ProxyId;
    }
}
