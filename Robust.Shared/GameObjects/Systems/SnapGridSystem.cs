using Robust.Shared.GameObjects.Components.Transform;

namespace Robust.Shared.GameObjects.Systems
{
    public class SnapGridSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MoveEvent>(OnMoveEvent);
        }

        private void OnMoveEvent(MoveEvent @event)
        {
            if (@event.Sender.TryGetComponent(out SnapGridComponent? snapGrid))
            {
                snapGrid.UpdatePosition();
            }
        }
    }
}
