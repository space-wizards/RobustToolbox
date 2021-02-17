using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    internal sealed class ContainerSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<EntParentChangedMessage>(HandleParentChanged);
        }

        // Eject entities from their parent container if the parent change is done by the transform only.
        private static void HandleParentChanged(EntParentChangedMessage message)
        {
            var oldParentEntity = message.OldParent;

            if (oldParentEntity == null || !oldParentEntity.IsValid())
                return;

            if (oldParentEntity.TryGetComponent(out IContainerManager? containerManager))
            {
                containerManager.ForceRemove(message.Entity);
            }
        }
    }
}
