using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Robust.Shared.Timing
{
    public interface IPauseManager
    {
        void SetMapPaused(MapId mapId, bool paused);

        void DoMapInitialize(MapId mapId);

        void DoGridMapInitialize(GridId gridId);
        void DoGridMapInitialize(IMapGrid grid);

        void AddUninitializedMap(MapId mapId);

        [Pure]
        bool IsMapPaused(MapId mapId);

        [Pure]
        bool IsGridPaused(IMapGrid grid);

        [Pure]
        bool IsGridPaused(GridId gridId);

        [Pure]
        bool IsMapInitialized(MapId mapId);
    }
}
