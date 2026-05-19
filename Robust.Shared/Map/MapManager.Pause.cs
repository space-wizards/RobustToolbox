using System.Globalization;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Map
{
    internal partial class MapManager
    {
        public void SetMapPaused(MapId mapId, bool paused)
        {
            _mapSystem.SetPaused(mapId, paused);
        }

        public void SetMapPaused(EntityUid uid, bool paused)
        {
            _mapSystem.SetPaused(uid, paused);
        }

        public void DoMapInitialize(MapId mapId)
        {
            _mapSystem.InitializeMap(mapId);
        }

        public bool IsMapInitialized(MapId mapId)
        {
            return _mapSystem.IsInitialized(mapId);
        }

        /// <inheritdoc />
        public bool IsMapPaused(MapId mapId)
        {
            return _mapSystem.IsPaused(mapId);
        }

        /// <inheritdoc />
        public bool IsMapPaused(EntityUid uid)
        {
            return _mapSystem.IsPaused(uid);
        }
    }
}
