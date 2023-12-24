using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

internal sealed partial class PvsSystem
{
    /// <summary>
    /// If PVS disabled then we'll track if we've dumped all entities on the player.
    /// This way any future ticks can be orders of magnitude faster as we only send what changes.
    /// </summary>
    private HashSet<ICommonSession> _seenAllEnts = new();

    private readonly Dictionary<ICommonSession, PvsSession> _playerData = new();

    internal PvsSession GetSessionData(ICommonSession session)
        => GetSessionData(_playerData[session]);

    internal PvsSession GetSessionData(PvsSession session)
    {
        UpdateSessionData(session);

        if (CullingEnabled && !session.DisableCulling)
            GetEntityStates(session);
        else
            GetAllEntityStates(session);

        _playerManager.GetPlayerStates(session.FromTick, session.PlayerStates);

        // lastAck varies with each client based on lag and such, we can't just make 1 global state and send it to everyone

        DebugTools.Assert(session.States.Select(x=> x.NetEntity).ToHashSet().Count == session.States.Count);
        DebugTools.AssertNull(session.State);
        session.State = new GameState(
            session.FromTick,
            _gameTiming.CurTick,
            Math.Max(session.LastInput, session.LastMessage),
            session.States,
            session.PlayerStates,
            _deletedEntities);

        if (_gameTiming.CurTick.Value > session.LastReceivedAck.Value + ForceAckThreshold)
            session.State.ForceSendReliably = true;

        return session;
    }
    internal void UpdateSessionData(PvsSession session)
    {
        DebugTools.AssertEqual(session.LeftView.Count, 0);
        DebugTools.AssertEqual(session.PlayerStates.Count, 0);
        DebugTools.AssertEqual(session.States.Count, 0);
        DebugTools.Assert(CullingEnabled && !session.DisableCulling || session.VisibleChunks.Count == 0);
        DebugTools.AssertNull(session.ToSend);
        DebugTools.AssertNull(session.State);

        session.FromTick = session.RequestedFull ? GameTick.Zero : session.LastReceivedAck;
        session.LastInput = _input.GetLastInputCommand(session.Session);
        session.LastMessage = _netEntMan.GetLastMessageSequence(session.Session);
        session.VisMask = EyeComponent.DefaultVisibilityMask;

        // Update visibility masks & viewer positions
        // TODO PVS do this before sending state.
        // I,e, we already enumerate over all eyes when computing visible chunks.
        Span<MapCoordinates> positions = stackalloc MapCoordinates[session.Viewers.Length];
        int i = 0;
        foreach (var viewer in session.Viewers)
        {
            if (viewer.Comp2 != null)
                session.VisMask |= viewer.Comp2.VisibilityMask;

            positions[i++] = _transform.GetMapCoordinates(viewer.Owner, viewer.Comp1);
        }

        if (!CullingEnabled || session.DisableCulling)
            return;

        // Sort chunks based on distance to the viewer
        i = 0;
        var nChunks = session.VisibleChunks.Count;
        Array.Resize(ref session.Chunks, nChunks);
        Array.Resize(ref session.ChunkDistanceSq, nChunks);
        foreach (var location in session.VisibleChunks)
        {
            if (!_chunks.TryGetValue(location, out var chunk))
            {
                session.Chunks[i++] = null;
                continue;
            }

            session.Chunks[i] = chunk;

            ref var dist = ref session.ChunkDistanceSq[i++];
            dist = float.MaxValue;

            foreach (var pos in positions)
            {
                if (pos.MapId == chunk.Position.MapId)
                    dist = Math.Min(dist, (pos.Position - chunk.Position.Position).LengthSquared());
            }
        }

        Array.Sort(session.ChunkDistanceSq, session.Chunks);
        session.VisibleChunks.Clear();
        session.ToSend = _entDataListPool.Get();

        if (session.PreviouslySent.TryGetValue(_gameTiming.CurTick - 1, out var lastSent))
            session.LastSent = (_gameTiming.CurTick, lastSent);
    }
}
