using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects
{
    public class SharedAudioSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        protected EntityCoordinates GetFallbackCoordinates(MapCoordinates mapCoordinates)
        {
            if (_mapManager.TryFindGridAt(mapCoordinates, out var mapGrid))
            {
                return new EntityCoordinates(mapGrid.GridEntityId,
                    mapGrid.WorldToLocal(mapCoordinates.Position));
            }

            if (_mapManager.HasMapEntity(mapCoordinates.MapId))
            {
                return new EntityCoordinates(_mapManager.GetMapEntityId(mapCoordinates.MapId),
                    mapCoordinates.Position);
            }

            return EntityCoordinates.Invalid;
        }
    }
}
