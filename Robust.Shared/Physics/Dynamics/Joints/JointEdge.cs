using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    /// <summary>
    /// A joint edge is used to connect bodies and joints together
    /// in a joint graph where each body is a node and each joint
    /// is an edge. A joint edge belongs to a doubly linked list
    /// maintained in each attached body. Each joint has two joint
    /// nodes, one for each attached body.
    /// </summary>
    public sealed class JointEdge
    {
        /// <summary>
        /// The joint.
        /// </summary>
        public Joint Joint { get; set; } = default!;

        /// <summary>
        /// The next joint edge in the body's joint list.
        /// </summary>
        public JointEdge? Next { get; set; }

        /// <summary>
        /// Provides quick access to the other body attached.
        /// </summary>
        public PhysicsComponent Other { get; set; } = default!;

        /// <summary>
        /// The previous joint edge in the body's joint list.
        /// </summary>
       public JointEdge? Prev { get; set; }
    }
}
