using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Robust.Shared.Physics
{
    public sealed class JointAddedEvent : EntityEventArgs
    {
        public Joint Joint { get; }

        public JointAddedEvent(Joint joint)
        {
            Joint = joint;
        }
    }

    public sealed class JointRemovedEvent : EntityEventArgs
    {
        public Joint Joint { get; }

        public JointRemovedEvent(Joint joint)
        {
            Joint = joint;
        }
    }
}
