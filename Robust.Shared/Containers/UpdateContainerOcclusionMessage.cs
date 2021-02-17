using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    public readonly struct UpdateContainerOcclusionMessage
    {
        public UpdateContainerOcclusionMessage(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }
}
