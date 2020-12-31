using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Chunks;
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
        private Dictionary<IPlayerSession, PlayerLookupChunks> _lastSeen = new();

        public override void Initialize()
        {
            base.Initialize();
            _playerManager.PlayerStatusChanged += HandlePlayerStatusChanged;
            SubscribeLocalEvent<DirtyEntityMessage>(HandleDirtyEntity);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= HandlePlayerStatusChanged;
            UnsubscribeLocalEvent<DirtyEntityMessage>();
        }

        private void HandleDirtyEntity(DirtyEntityMessage message)
        {
            // Removing from lookup should be handled elsewhere already.
            if (message.Entity.Deleted) return;

            var aabb = EntityManager.GetWorldAabbFromEntity(message.Entity);
            var mapId = message.Entity.Transform.MapID;

            foreach (var chunk in GetChunksInRange(mapId, aabb))
            {
                chunk.LastModifiedTick = _gameTiming.CurTick;
            }
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
    public Dictionary<EntityUid, GameTick> EntityLastSeen { get; set; } = new();

    public readonly Dictionary<EntityLookupChunk, GameTick> KnownChunks = new();

    public bool TryLastSeen(EntityUid uid, out GameTick lastSeen)
    {
        if (EntityLastSeen.TryGetValue(uid, out lastSeen))
        {
            return true;
        }

        return false;
    }

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

        return;
    }

    public void RemoveChunk(EntityLookupChunk chunk)
    {
        KnownChunks.Remove(chunk);
    }
}
