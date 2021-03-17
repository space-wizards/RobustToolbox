namespace Robust.Shared.GameObjects
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
