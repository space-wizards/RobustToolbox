using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    public readonly struct UpdateContainerOcclusionMessage
    {
        public IEntity Entity { get; }

        public UpdateContainerOcclusionMessage(IEntity entity)
        {
            Entity = entity;
        }
    }
}
