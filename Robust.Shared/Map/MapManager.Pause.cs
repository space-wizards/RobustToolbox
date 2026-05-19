using System.Globalization;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Map
{
    internal partial class MapManager
    {
        public void SetMapPaused(MapId mapId, bool paused)
        {
            MapSystem.SetPaused(mapId, paused);
        }

        public void SetMapPaused(EntityUid uid, bool paused)
        {
            MapSystem.SetPaused(uid, paused);
        }

        public void DoMapInitialize(MapId mapId)
        {
            MapSystem.InitializeMap(mapId);
        }

        public bool IsMapInitialized(MapId mapId)
        {
            return MapSystem.IsInitialized(mapId);
        }

        /// <inheritdoc />
        public bool IsMapPaused(MapId mapId)
        {
            return MapSystem.IsPaused(mapId);
        }

        /// <inheritdoc />
        public bool IsMapPaused(EntityUid uid)
        {
            return MapSystem.IsPaused(uid);
        }
    }
}
