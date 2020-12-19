namespace Robust.Shared.Physics
{
    public sealed class PhysicsMapCallback
    {
        public delegate void BroadphaseDelegate(int proxyIdA, int proxyIdB);
    }
}
