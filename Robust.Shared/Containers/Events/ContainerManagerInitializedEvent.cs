using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers.Events
{
    public struct ContainerManagerInitializedEvent
    {
        public EntityUid Uid;
        public ContainerManagerComponent Component;
    }
}
