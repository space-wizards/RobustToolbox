using Robust.Shared.Physics.Dynamics.Contacts;

namespace Robust.Shared.Physics.Collision
{
    /// <summary>
    /// Input for Distance.ComputeDistance().
    /// You have to option to use the shape radii in the computation.
    /// </summary>
    internal sealed class DistanceInput
    {
        public DistanceProxy ProxyA = new();
        public DistanceProxy ProxyB = new();
        public Transform TransformA;
        public Transform TransformB;
        public bool UseRadii;
    }
}
