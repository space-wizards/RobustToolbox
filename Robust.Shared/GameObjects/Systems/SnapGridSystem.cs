using JetBrains.Annotations;

namespace Robust.Shared.GameObjects
{
    [UsedImplicitly]
    public class SnapGridSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SnapGridComponent, ComponentInit>(HandleComponentInit);
            SubscribeLocalEvent<SnapGridComponent, ComponentShutdown>(HandleComponentShutdown);
            SubscribeLocalEvent<SnapGridComponent, MoveEvent>(HandleMoveEvent);
        }
        
        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<SnapGridComponent, ComponentInit>(HandleComponentInit);
            UnsubscribeLocalEvent<SnapGridComponent, ComponentShutdown>(HandleComponentShutdown);
            UnsubscribeLocalEvent<SnapGridComponent, MoveEvent>(HandleMoveEvent);
        }

        private void HandleComponentInit(EntityUid uid, SnapGridComponent component, ComponentInit args)
        {
            SnapGridComponent.UpdatePosition(component);
        }

        private void HandleComponentShutdown(EntityUid uid, SnapGridComponent component, ComponentShutdown args)
        {
            SnapGridComponent.CompShutdown(component);
        }

        private void HandleMoveEvent(EntityUid uid, SnapGridComponent snapGrid, MoveEvent args)
        {
            SnapGridComponent.UpdatePosition(snapGrid);
        }
    }
}
