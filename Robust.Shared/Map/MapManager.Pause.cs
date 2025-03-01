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

        /// <summary>
        /// Initializes the map pausing system.
        /// </summary>
        private void InitializeMapPausing()
        {
            _conhost.RegisterCommand("pausemap",
                "Pauses a map, pausing all simulation processing on it.",
                "pausemap <map ID>",
                (shell, _, args) =>
                {
                    if (args.Length != 1)
                    {
                        shell.WriteError("Need to supply a valid MapId");
                        return;
                    }

                    var mapId = new MapId(int.Parse(args[0], CultureInfo.InvariantCulture));

                    if (!MapExists(mapId))
                    {
                        shell.WriteError("That map does not exist.");
                        return;
                    }

                    SetMapPaused(mapId, true);
                });

            _conhost.RegisterCommand("querymappaused",
                "Check whether a map is paused or not.",
                "querymappaused <map ID>",
                (shell, _, args) =>
                {
                    var mapId = new MapId(int.Parse(args[0], CultureInfo.InvariantCulture));

                    if (!MapExists(mapId))
                    {
                        shell.WriteError("That map does not exist.");
                        return;
                    }

                    shell.WriteLine(_mapSystem.IsPaused(mapId).ToString());
                });

            _conhost.RegisterCommand("unpausemap",
                "unpauses a map, resuming all simulation processing on it.",
                "Usage: unpausemap <map ID>",
                (shell, _, args) =>
                {
                    if (args.Length != 1)
                    {
                        shell.WriteLine("Need to supply a valid MapId");
                        return;
                    }

                    var mapId = new MapId(int.Parse(args[0], CultureInfo.InvariantCulture));

                    if (!MapExists(mapId))
                    {
                        shell.WriteLine("That map does not exist.");
                        return;
                    }

                    SetMapPaused(mapId, false);
                });
        }
    }
}
