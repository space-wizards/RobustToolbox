using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers.Events
{
    public struct ContainerManagerShutdownEvent
    {
        public EntityUid Uid;
        public ContainerManagerComponent Component;
    }
}
