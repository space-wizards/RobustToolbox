using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects
{
    [UsedImplicitly]
    internal sealed class MapSystem : EntitySystem
    {
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MapGridComponent, ComponentRemove>(RemoveHandler);
            SubscribeLocalEvent<MapGridComponent, ComponentInit>(HandleGridInitialize);
        }

        private void RemoveHandler(EntityUid uid, MapGridComponent component, ComponentRemove args)
        {
            _mapManager.OnComponentRemoved(component);
        }

        private void HandleGridInitialize(EntityUid uid, MapGridComponent component, ComponentInit args)
        {
            var msg = new GridInitializeEvent(uid, component.GridIndex);
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);
        }
    }

    /// <summary>
    /// Raised whenever a grid is being initialized.
    /// </summary>
    public sealed class GridInitializeEvent : EntityEventArgs
    {
        public EntityUid EntityUid { get; }
        public GridId GridId { get; }

        public GridInitializeEvent(EntityUid uid, GridId gridId)
        {
            EntityUid = uid;
            GridId = gridId;
        }
    }
}
