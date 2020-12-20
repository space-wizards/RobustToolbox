using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Broadphase
{
    internal sealed class PhysicsLookupNode
    {
        internal PhysicsLookupChunk ParentChunk { get; }

        internal Vector2i Indices { get; }

        internal IReadOnlyCollection<FixtureProxy> Proxies => _proxies;

        private readonly List<FixtureProxy> _proxies = new List<FixtureProxy>();

        internal PhysicsLookupNode(PhysicsLookupChunk parentChunk, Vector2i indices)
        {
            ParentChunk = parentChunk;
            Indices = indices;
        }

        internal void AddProxy(FixtureProxy proxy)
        {
            DebugTools.Assert(!_proxies.Contains(proxy));
            _proxies.Add(proxy);
        }

        internal void RemoveProxy(FixtureProxy proxy)
        {
            DebugTools.Assert(_proxies.Contains(proxy));
            _proxies.Remove(proxy);
        }
    }
}
