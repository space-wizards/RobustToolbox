using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Chunks;
using Robust.Shared.Timing;

namespace Robust.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class ServerEntityLookupSystem : SharedEntityLookupSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        /// <summary>
        ///     Last tick the player saw a particular entity.
        /// </summary>
        private Dictionary<IPlayerSession, PlayerLookupChunks> _lastSeen =
            new Dictionary<IPlayerSession, PlayerLookupChunks>();

        public override void Initialize()
        {
            base.Initialize();
            _playerManager.PlayerStatusChanged += HandlePlayerStatusChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= HandlePlayerStatusChanged;
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
    public Dictionary<EntityUid, GameTick> EntityLastSeen { get; set; } = new Dictionary<EntityUid, GameTick>();

    public readonly Dictionary<EntityLookupChunk, GameTick> KnownChunks =
        new Dictionary<EntityLookupChunk, GameTick>();

    public bool TryLastSeen(EntityUid uid, out GameTick lastSeen)
    {
        if (EntityLastSeen.TryGetValue(uid, out lastSeen))
        {
            return true;
        }

        return false;
    }

    public GameTick LastSeen(EntityLookupChunk chunk)
    {
        return KnownChunks[chunk];
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
