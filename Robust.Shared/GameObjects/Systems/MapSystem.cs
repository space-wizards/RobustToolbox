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
        }

        private void RemoveHandler(EntityUid uid, MapGridComponent component, ComponentRemove args)
        {
            _mapManager.OnComponentRemoved(component);
        }
    }
}
