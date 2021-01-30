namespace Robust.Shared.Physics.Collision
{
    /// <summary>
    /// Used to warm start ComputeDistance.
    /// Set count to zero on first call.
    /// </summary>
    internal struct SimplexCache
    {
        /// <summary>
        /// Length or area
        /// </summary>
        public ushort Count;

        /// <summary>
        /// Vertices on shape A
        /// </summary>
        public unsafe fixed byte IndexA[3];

        /// <summary>
        /// Vertices on shape B
        /// </summary>
        public unsafe fixed byte IndexB[3];

        public float Metric;
    }
}
