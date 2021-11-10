using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects
{
    [UsedImplicitly]
    internal abstract class SharedMapSystem : EntitySystem
    {
        [Dependency] protected readonly IMapManagerInternal MapManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MapGridComponent, ComponentRemove>(RemoveHandler);
            SubscribeLocalEvent<MapGridComponent, ComponentInit>(HandleGridInitialize);
            SubscribeLocalEvent<MapGridComponent, ComponentStartup>(HandleGridStartup);
        }

        private void HandleGridStartup(EntityUid uid, MapGridComponent component, ComponentStartup args)
        {
            var msg = new GridStartupEvent(uid, component.GridIndex);
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);
        }

        private void RemoveHandler(EntityUid uid, MapGridComponent component, ComponentRemove args)
        {
            EntityManager.EventBus.RaiseLocalEvent(uid, new GridRemovalEvent(uid, component.GridIndex));
            MapManager.OnComponentRemoved(component);
        }

        private void HandleGridInitialize(EntityUid uid, MapGridComponent component, ComponentInit args)
        {
            var msg = new GridInitializeEvent(uid, component.GridIndex);
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);
        }
    }

    public sealed class GridStartupEvent : EntityEventArgs
    {
        public EntityUid EntityUid { get; }
        public GridId GridId { get; }

        public GridStartupEvent(EntityUid uid, GridId gridId)
        {
            EntityUid = uid;
            GridId = gridId;
        }
    }

    public sealed class GridRemovalEvent : EntityEventArgs
    {
        public EntityUid EntityUid { get; }
        public GridId GridId { get; }

        public GridRemovalEvent(EntityUid uid, GridId gridId)
        {
            EntityUid = uid;
            GridId = gridId;
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
