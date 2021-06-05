using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Robust.Shared.Physics
{
    public sealed class JointAddedEvent : EntityEventArgs
    {
        public IPhysBody OurBody { get; }

        public IPhysBody OtherBody { get; }

        public Joint Joint { get; }

        public JointAddedEvent(Joint joint, IPhysBody ourBody, IPhysBody otherBody)
        {
            Joint = joint;
            OurBody = ourBody;
            OtherBody = otherBody;
        }
    }

    public sealed class JointRemovedEvent : EntityEventArgs
    {
        public IPhysBody OurBody { get; }

        public IPhysBody OtherBody { get; }

        public Joint Joint { get; }

        public JointRemovedEvent(Joint joint, IPhysBody ourBody, IPhysBody otherBody)
        {
            Joint = joint;
            OurBody = ourBody;
            OtherBody = otherBody;
        }
    }
}
