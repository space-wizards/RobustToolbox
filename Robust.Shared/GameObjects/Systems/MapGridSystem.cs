using Robust.Shared.Map;

namespace Robust.Shared.GameObjects
{
    internal sealed class MapGridSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MapGridComponent, ComponentInit>(HandleGridInitialize);
        }

        private void HandleGridInitialize(EntityUid uid, MapGridComponent component, ComponentInit args)
        {
            var msg = new GridInitializedEvent(uid, component.GridIndex);
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);
        }
    }

    public sealed class GridInitializedEvent : EntityEventArgs
    {
        public EntityUid EntityUid { get; }
        public GridId GridId { get; }

        public GridInitializedEvent(EntityUid uid, GridId gridId)
        {
            EntityUid = uid;
            GridId = gridId;
        }
    }
}
