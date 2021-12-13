using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    public readonly struct UpdateContainerOcclusionMessage
    {
        public EntityUid Entity { get; }

        public UpdateContainerOcclusionMessage(EntityUid entity)
        {
            Entity = entity;
        }
    }
}
