using JetBrains.Annotations;

namespace Robust.Shared.GameObjects
{
    [UsedImplicitly]
    internal class SnapGridSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SnapGridComponent, ComponentStartup>(HandleComponentStartup);
        }

        private void HandleComponentStartup(EntityUid uid, SnapGridComponent component, ComponentStartup args)
        {
            var transform = ComponentManager.GetComponent<ITransformComponent>(uid);
            transform.Anchored = true;

            // Remove us, we have been migrated.
            ComponentManager.RemoveComponent<SnapGridComponent>(uid);
        }
    }
}
