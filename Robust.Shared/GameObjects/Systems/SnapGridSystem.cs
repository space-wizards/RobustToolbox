using JetBrains.Annotations;

namespace Robust.Shared.GameObjects
{
    [UsedImplicitly]
    public class SnapGridSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SnapGridComponent, MoveEvent>(OnMoveEvent);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<SnapGridComponent, MoveEvent>(OnMoveEvent);
        }

        private void OnMoveEvent(EntityUid uid, SnapGridComponent snapGrid, MoveEvent args)
        {
            snapGrid.UpdatePosition();
        }
    }
}
