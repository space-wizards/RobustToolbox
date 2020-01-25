using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    internal delegate bool QueryCallback(int proxyId);

    /// <summary>
    /// Callback predicate for when a ray cast hits an entity.
    /// </summary>
    /// <param name="proxy">Id of the proxy that was hit.</param>
    /// <param name="results">Information about the ray cast intersection.</param>
    /// <returns>Should the entity be accepted?</returns>
    internal delegate bool RayCastCallback(int proxy, RayCastResults results);

    internal delegate void BroadPhaseCallback(int proxyA, int proxyB);

    internal struct BodyProxy
    {
        public IPhysBody Body;
    }

    internal interface IBroadPhase
    {
        // Create
        int AddProxy(in BodyProxy proxy);

        // Read
        BodyProxy GetProxy(int proxyId);

        // Update
        void MoveProxy(int proxyId, ref Box2 aabb, Vector2 displacement);
        void SetProxy(int proxyId, ref BodyProxy proxy);

        // Delete
        void RemoveProxy(int proxyId);

        // Query
        void Query(QueryCallback callback, in Box2 aabb);

        // Test
        bool Test(int proxyA, int proxyB);

        // Raycast
        void RayCast(RayCastCallback callback, MapId mapId, in Ray ray, float maxLength = 25);

        // Update
        void Update(BroadPhaseCallback callback);
    }
}
