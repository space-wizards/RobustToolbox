using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    public abstract class SharedContainerSystem : EntitySystem
    {
        public readonly Dictionary<EntityUid, IContainer> ExpectedEntities = new();

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntParentChangedMessage>(HandleParentChanged);
        }

        // Eject entities from their parent container if the parent change is done by the transform only.
        private static void HandleParentChanged(EntParentChangedMessage message)
        {
            var oldParentEntity = message.OldParent;

            if (oldParentEntity == null || !oldParentEntity.IsValid())
                return;

            if (oldParentEntity.TryGetComponent(out IContainerManager? containerManager))
                containerManager.ForceRemove(message.Entity);
        }

        public void AddExpectedEntity(EntityUid uid, IContainer container)
        {
            if (ExpectedEntities.ContainsKey(uid))
                return;
            ExpectedEntities.Add(uid, container);
            container.ExpectedEntities.Add(uid);
        }

        public void RemoveExpectedEntity(EntityUid uid)
        {
            if (!ExpectedEntities.ContainsKey(uid))
                return;
            var container = ExpectedEntities[uid];
            ExpectedEntities.Remove(uid);
            container.ExpectedEntities.Add(uid);
        }
    }
}
