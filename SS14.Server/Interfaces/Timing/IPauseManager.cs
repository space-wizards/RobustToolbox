using JetBrains.Annotations;
using SS14.Server.GameObjects.Components.Markers;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Map;

namespace SS14.Server.Interfaces.Timing
{
    public interface IPauseManager
    {
        void SetMapPaused(IMap map, bool paused);
        void SetMapPaused(MapId mapId, bool paused);

        [Pure]
        bool IsMapPaused(IMap map);

        [Pure]
        bool IsMapPaused(MapId mapId);

        [Pure]
        bool IsGridPaused(IMapGrid grid);

        [Pure]
        bool IsGridPaused(GridId gridId);
    }

    public static class PauseManagerExt
    {
        [Pure]
        public static bool IsEntityPaused(this IPauseManager manager, IEntity entity)
        {
            return !entity.HasComponent<IgnorePauseComponent>() && manager.IsGridPaused(entity.Transform.GridID);
        }
    }
}
