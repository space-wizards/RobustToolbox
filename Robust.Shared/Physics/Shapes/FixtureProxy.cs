using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     Connects fixtures to the broadphase
    /// </summary>
    /*
     * We might be able to end up removing this but for now I've just left as is. Its purpose is to store swept fixture AABBs
     * as every time a fixture moves it needs to have its AABB re-calculated so we're churning through these.
     */
    public struct FixtureProxy
    {
        public Box2 AABB;
        public int ChildIndex;
        public Fixture Fixture;
        public int ProxyId;
    }
}
