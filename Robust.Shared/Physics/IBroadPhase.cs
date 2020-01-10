using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    internal delegate bool QueryCallback(int proxyId);

    internal delegate bool RayCastCallback(RayCastResults results);

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
        void RayCast(RayCastCallback callback, in Ray ray, float maxLength = 25);

        // Update
        void Update(BroadPhaseCallback callback);
    }
}
