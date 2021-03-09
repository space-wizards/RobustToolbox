using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.EntityLookup;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Robust.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class EntityLookupSystem : SharedEntityLookupSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        /// <summary>
        ///     Last tick the player saw a particular entity.
        /// </summary>
        private Dictionary<ICommonSession, PlayerLookupChunks> _lastSeen = new();

        private HashSet<ICommonSession> _debugSubscribed = new();

        private HashSet<EntityUid> _handledDirty = new();

        public override void Initialize()
        {
            base.Initialize();
            _playerManager.PlayerStatusChanged += HandlePlayerStatusChanged;
            SubscribeLocalEvent<DirtyEntityMessage>(HandleDirtyEntity);
            SubscribeLocalEvent<ChunkSubscribeMessage>(HandleSubscribe);
            SubscribeLocalEvent<ChunkUnsubscribeMessage>(HandleUnsubscribe);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _handledDirty.Clear();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= HandlePlayerStatusChanged;
            UnsubscribeLocalEvent<DirtyEntityMessage>();
            UnsubscribeLocalEvent<ChunkSubscribeMessage>();
            UnsubscribeLocalEvent<ChunkUnsubscribeMessage>();
        }

        private void HandleSubscribe(ChunkSubscribeMessage message)
        {
            _debugSubscribed.Add(message.Session);
        }

        private void HandleUnsubscribe(ChunkUnsubscribeMessage message)
        {
            _debugSubscribed.Remove(message.Session);
        }

        protected override void HandleEntityDeleted(EntityDeletedMessage message)
        {
            base.HandleEntityDeleted(message);
            foreach (var (_, data) in _lastSeen)
            {
                data.EntityLastSeen.Remove(message.Entity.Uid);
            }
        }

        protected override void RemoveChunk(EntityLookupChunk chunk)
        {
            base.RemoveChunk(chunk);
            foreach (var (_, data) in _lastSeen)
            {
                data.KnownChunks.Remove(chunk);
            }
        }

        private void HandleDirtyEntity(DirtyEntityMessage message)
        {
            // Removing from lookup should be handled elsewhere already.
            // As the message can be raised multiple times we'll just check if we've already handled it this tick.
            if (message.Entity.Deleted || !_handledDirty.Add(message.Entity.Uid)) return;

            var entity = message.Entity;

            while (true)
            {
                if (entity.TryGetContainer(out var container))
                {
                    entity = container.Owner;
                    continue;
                }

                break;
            }

            if (!LastKnownNodes.TryGetValue(entity, out var nodes))
                return;

            var currentTick = _gameTiming.CurTick;

            foreach (var node in nodes)
            {
                node.ParentChunk.LastModifiedTick = currentTick;
            }

#if DEBUG
            if (_debugSubscribed.Count > 0)
            {
                var chunks = new Dictionary<MapId, Dictionary<GridId, List<Vector2i>>>();

                foreach (var node in nodes)
                {
                    if (!chunks.TryGetValue(node.ParentChunk.MapId, out var map))
                    {
                        map = new Dictionary<GridId, List<Vector2i>>();
                        chunks[node.ParentChunk.MapId] = map;
                    }

                    if (!map.TryGetValue(node.ParentChunk.GridId, out var grid))
                    {
                        grid = new List<Vector2i>();
                        map[node.ParentChunk.GridId] = grid;
                    }

                    if (!grid.Contains(node.ParentChunk.Origin))
                    {
                        grid.Add(node.ParentChunk.Origin);
                    }
                }

                foreach (var player in _debugSubscribed)
                {
                    var debugMessage = new ChunkDirtyMessage(chunks);
                    RaiseNetworkEvent(debugMessage, player.ConnectedClient);
                }
            }
#endif
        }

        private void HandlePlayerStatusChanged(object? sender, SessionStatusEventArgs eventArgs)
        {
            if (eventArgs.NewStatus != SessionStatus.InGame)
            {
                _lastSeen.Remove(eventArgs.Session);
                return;
            }

            if (!_lastSeen.ContainsKey(eventArgs.Session))
                _lastSeen[eventArgs.Session] = new PlayerLookupChunks();
        }

        internal PlayerLookupChunks? GetPlayerLastSeen(IPlayerSession session)
        {
            return !_lastSeen.TryGetValue(session, out var chunks) ? null : chunks;
        }
    }
}

internal sealed class PlayerLookupChunks
{
    /// <summary>
    ///     Save the list to avoid having to create a new one every time.
    /// </summary>
    public List<EntityState> EntityStates { get; } = new(256);

    public Dictionary<EntityUid, GameTick> EntityLastSeen { get; } = new();

    public readonly Dictionary<EntityLookupChunk, GameTick> KnownChunks = new();

    public GameTick? LastSeen(EntityLookupChunk chunk)
    {
        if (KnownChunks.TryGetValue(chunk, out var lastTick))
            return lastTick;

        return null;
    }

    public void UpdateChunk(GameTick currentTick, EntityLookupChunk chunk)
    {
        if (KnownChunks.TryGetValue(chunk, out var last) && last >= chunk.LastModifiedTick)
            return;

        KnownChunks[chunk] = currentTick;
    }
}
