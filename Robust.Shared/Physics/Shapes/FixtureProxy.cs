using System;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     Connects fixtures to the broadphase
    /// </summary>
    public struct FixtureProxy
    {
        public Box2 AABB;
        public int ChildIndex;
        public Fixture Fixture;
        public DynamicTree.Proxy ProxyId;
    }
}
