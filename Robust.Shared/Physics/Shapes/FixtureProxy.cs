using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     Connects fixtures to the broadphase
    /// </summary>
    public struct FixtureProxy
    {
        public AABB AABB;
        public int ChildIndex;
        public Fixture Fixture;
        public int ProxyId;
    }
}
