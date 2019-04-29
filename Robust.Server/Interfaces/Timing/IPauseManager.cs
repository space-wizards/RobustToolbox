using JetBrains.Annotations;
using Robust.Server.GameObjects.Components.Markers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;

namespace Robust.Server.Interfaces.Timing
{
    public interface IPauseManager
    {
        void SetMapPaused(IMap map, bool paused);
        void SetMapPaused(MapId mapId, bool paused);

        void DoMapInitialize(MapId mapId);
        void DoMapInitialize(IMap map);

        void DoGridMapInitialize(GridId gridId);
        void DoGridMapInitialize(IMapGrid grid);

        void AddUninitializedMap(MapId mapId);
        void AddUninitializedMap(IMap map);

        [Pure]
        bool IsMapPaused(IMap map);

        [Pure]
        bool IsMapPaused(MapId mapId);

        [Pure]
        bool IsGridPaused(IMapGrid grid);

        [Pure]
        bool IsGridPaused(GridId gridId);

        [Pure]
        bool IsMapInitialized(MapId mapId);

        [Pure]
        bool IsMapInitialized(IMap map);
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
