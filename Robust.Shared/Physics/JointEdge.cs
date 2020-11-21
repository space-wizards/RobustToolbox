namespace Robust.Shared.Physics
{
    /// <summary>
    ///     Used to connect bodies and joints together in a graph where each body is a node and each joint is an edge.
    /// </summary>
    internal sealed class JointEdge
    {
        internal Joint Joint { get; set; } = default!;

        internal JointEdge Next { get; set; } = default!;

        /// <summary>
        ///     The other body attached to this edge
        /// </summary>
        internal IPhysBody? Other { get; set; }

        internal JointEdge Previous { get; set; } = default!;
    }
}
