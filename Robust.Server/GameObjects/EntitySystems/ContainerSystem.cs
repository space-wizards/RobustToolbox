using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Server.GameObjects.EntitySystems
{
    internal sealed class ContainerSystem : EntitySystem
    {
        public override void SubscribeEvents()
        {
            base.SubscribeEvents();

            SubscribeEvent<EntParentChangedMessage>(HandleParentChanged);
        }

        // Eject entities from their parent container if the parent change is done by the transform only.
        private static void HandleParentChanged(object sender, EntParentChangedMessage message)
        {
            var oldParentEntity = message.OldParent;

            if (oldParentEntity == null || !oldParentEntity.IsValid())
                return;

            if (oldParentEntity.TryGetComponent(out IContainerManager containerManager))
            {
                containerManager.ForceRemove(message.Entity);
            }
        }
    }
}
