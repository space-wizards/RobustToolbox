using System;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Robust.Shared.Timing
{
    [Obsolete("Use the same functions on IMapManager.")]
    public interface IPauseManager
    {
        void SetMapPaused(MapId mapId, bool paused);

        void DoMapInitialize(MapId mapId);

        [Obsolete("This function does nothing, per-grid pausing isn't a thing anymore.")]
        void DoGridMapInitialize(GridId gridId);

        [Obsolete("This function does nothing, per-grid pausing isn't a thing anymore.")]
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
