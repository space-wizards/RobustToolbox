namespace Robust.Shared.Physics
{
    /// <summary>
    ///     Used to connect bodies and joints together in a graph where each body is a node and each joint is an edge.
    /// </summary>
    public sealed class JointEdge
    {
        public Joint Joint { get; set; } = default!;

        public JointEdge Next { get; set; } = default!;

        /// <summary>
        ///     The other body attached to this edge
        /// </summary>
        public IPhysBody? Other { get; set; }

        public JointEdge Previous { get; set; } = default!;
    }
}
