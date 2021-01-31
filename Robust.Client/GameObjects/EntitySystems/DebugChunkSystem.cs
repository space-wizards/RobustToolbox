#if DEBUG
using System;
using System.Collections.Generic;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Player;
using Robust.Shared.EntityLookup;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects.EntitySystems
{
    internal sealed class DebugChunkSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        internal IReadOnlyDictionary<MapId, Dictionary<GridId, Dictionary<Vector2i, TimeSpan>>> Chunks => _chunks;
        private Dictionary<MapId, Dictionary<GridId, Dictionary<Vector2i, TimeSpan>>> _chunks = new();

        internal const float Duration = 1.0f;

        private ChunkOverlay _overlay = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<ChunkDirtyMessage>(HandleChunkDirty);
            _overlay = new ChunkOverlay(
                IoCManager.Resolve<IEyeManager>(),
                IoCManager.Resolve<IGameTiming>(),
                IoCManager.Resolve<IMapManager>(),
                IoCManager.Resolve<IPlayerManager>());
            IoCManager.Resolve<IOverlayManager>().AddOverlay(_overlay);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            UnsubscribeNetworkEvent<ChunkDirtyMessage>();
            IoCManager.Resolve<IOverlayManager>().RemoveOverlay(nameof(ChunkOverlay));
        }

        private void HandleChunkDirty(ChunkDirtyMessage message)
        {
            var currentTime = _gameTiming.CurTime;

            foreach (var (mapId, grids) in message.DirtyChunks)
            {
                if (!_chunks.TryGetValue(mapId, out var existingMap))
                {
                    existingMap = new Dictionary<GridId, Dictionary<Vector2i, TimeSpan>>();
                    _chunks[mapId] = existingMap;
                }

                foreach (var (gridId, chunks) in grids)
                {
                    if (!existingMap.TryGetValue(gridId, out var existingGrids))
                    {
                        existingGrids = new Dictionary<Vector2i, TimeSpan>();
                        existingMap[gridId] = existingGrids;
                    }

                    foreach (var chunk in chunks)
                    {
                        existingGrids[chunk] = currentTime;
                    }
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // It's ugly but it works
            if (_chunks.Count == 0) return;

            // Cleanup empties
            var toRemove = new List<Vector2i>();
            var currentTime = _gameTiming.CurTime;

            foreach (var (_, grids) in _chunks)
            {
                foreach (var (_, chunks) in grids)
                {
                    foreach (var (chunk, time) in chunks)
                    {
                        if (time.TotalSeconds + Duration > currentTime.TotalSeconds) continue;
                        toRemove.Add(chunk);
                    }
                }
            }


            foreach (var (_, grids) in _chunks)
            {
                foreach (var (_, chunks) in grids)
                {
                    foreach (var chunk in toRemove)
                    {
                        chunks.Remove(chunk);
                    }
                }
            }

            var toRemoveMaps = new List<MapId>();
            var toRemoveGrids = new List<GridId>();

            foreach (var (mapId, grids) in _chunks)
            {
                if (grids.Count == 0)
                {
                    toRemoveMaps.Add(mapId);
                    continue;
                }

                foreach (var (gridId, chunks) in grids)
                {
                    if (chunks.Count > 0) continue;
                    toRemoveGrids.Add(gridId);
                }
            }

            foreach (var (_, grids) in _chunks)
            {
                foreach (var grid in toRemoveGrids)
                {
                    grids.Remove(grid);
                }
            }

            foreach (var map in toRemoveMaps)
            {
                _chunks.Remove(map);
            }
        }
    }

    internal sealed class ChunkOverlay : Overlay
    {
        private readonly IEyeManager _eyeManager;
        private readonly IGameTiming _gameTiming;
        private readonly IMapManager _mapManager;
        private readonly IPlayerManager _playerManager;

        private DebugChunkSystem _debugChunks;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public ChunkOverlay(IEyeManager eyeManager, IGameTiming gameTiming, IMapManager mapManager, IPlayerManager playerManager) : base(nameof(ChunkOverlay))
        {
            _eyeManager = eyeManager;
            _gameTiming = gameTiming;
            _mapManager = mapManager;
            _playerManager = playerManager;
            _debugChunks = EntitySystem.Get<DebugChunkSystem>();
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            var player = _playerManager.LocalPlayer?.ControlledEntity;

            if (_debugChunks.Chunks.Count == 0 || player == null || !_debugChunks.Chunks.TryGetValue(player.Transform.MapID, out var grids)) return;

            var worldHandle = (DrawingHandleWorld) handle;
            var currentTime = _gameTiming.CurTime;
            var viewPort = _eyeManager.GetWorldViewport();

            foreach (var gridId in _mapManager.FindGridIdsIntersecting(player.Transform.MapID, viewPort, true))
            {
                if (!grids.TryGetValue(gridId, out var chunks)) continue;

                foreach (var (chunk, start) in chunks)
                {
                    var elapsed = currentTime - start;
                    var ratio = 1f - (float) (elapsed / DebugChunkSystem.Duration).TotalSeconds;

                    if (ratio <= 0f) continue;

                    var rect = new Box2(new Vector2(chunk.X, chunk.Y), new Vector2(chunk.X + EntityLookupChunk.ChunkSize, chunk.Y + EntityLookupChunk.ChunkSize));

                    worldHandle.DrawRect(rect, Color.Green.WithAlpha(ratio * 0.8f));
                }
            }
        }
    }
}
#endif
