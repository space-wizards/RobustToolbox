using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network.Messages;
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

    internal readonly Dictionary<ICommonSession, PvsSession> PlayerData = new();

    private List<ICommonSession> _disconnected = new();

    private void SendStateUpdate(ICommonSession session, PvsThreadResources resources)
    {
        var data = GetOrNewPvsSession(session);
        ComputeSessionState(data);

        InterlockedHelper.Min(ref _oldestAck, data.FromTick.Value);

        // actually send the state
        var msg = new MsgState
        {
            State = data.State,
            CompressionContext = resources.CompressionContext
        };

        // PVS benchmarks use dummy sessions.
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (session.Channel != null)
        {
            _netMan.ServerSendMessage(msg, session.Channel);
            if (msg.ShouldSendReliably())
            {
                data.RequestedFull = false;
                data.LastReceivedAck = _gameTiming.CurTick;
                lock (PendingAcks)
                {
                    PendingAcks.Add(session);
                }
            }
        }
        else
        {
            // Always "ack" dummy sessions.
            data.LastReceivedAck = _gameTiming.CurTick;
            data.RequestedFull = false;
            lock (PendingAcks)
            {
                PendingAcks.Add(session);
            }
        }

        data.ClearState();
    }

    private PvsSession GetOrNewPvsSession(ICommonSession session)
    {
        if (!PlayerData.TryGetValue(session, out var pvsSession))
        {
            var memoryRegion = CreateSessionDataMemory();
            PlayerData[session] = pvsSession = new(session, memoryRegion);
        }

        return pvsSession;
    }

    internal void ComputeSessionState(PvsSession session)
    {
        UpdateSession(session);

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

        session.State.ForceSendReliably = session.RequestedFull
                                          || _gameTiming.CurTick > session.LastReceivedAck + (uint) ForceAckThreshold;
    }

    private void UpdateSession(PvsSession session)
    {
        DebugTools.AssertEqual(session.LeftView.Count, 0);
        DebugTools.AssertEqual(session.PlayerStates.Count, 0);
        DebugTools.AssertEqual(session.States.Count, 0);
        DebugTools.Assert(CullingEnabled && !session.DisableCulling || session.Chunks.Count == 0);
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

        var chunks = session.Chunks;
        var distances = session.ChunkDistanceSq;
        distances.Clear();
        distances.EnsureCapacity(chunks.Count);

        // Assemble list of chunks and their distances to the nearest eye.
        foreach (ref var tuple in CollectionsMarshal.AsSpan(chunks))
        {
            var chunk = tuple.Chunk;
            var dist = float.MaxValue;
            var chebDist = float.MaxValue;

            DebugTools.Assert(!chunk.UpdateQueued);
            DebugTools.Assert(!chunk.Dirty);

            foreach (var pos in positions)
            {
                if (pos.MapId != chunk.Position.MapId)
                    continue;

                dist = Math.Min(dist, (pos.Position - chunk.Position.Position).LengthSquared());

                var relative = chunk.InvWorldMatrix.Transform(pos.Position)  - chunk.Centre;
                relative = Vector2.Abs(relative);
                chebDist = Math.Min(chebDist, Math.Max(relative.X, relative.Y));
            }

            distances.Add(dist);
            tuple.ChebyshevDistance = chebDist;
        }

        // Sort chunks based on distances
        CollectionsMarshal.AsSpan(distances).Sort(CollectionsMarshal.AsSpan(chunks));

        session.ToSend = _entDataListPool.Get();

        if (session.PreviouslySent.TryGetValue(_gameTiming.CurTick - 1, out var lastSent))
            session.LastSent = (_gameTiming.CurTick, lastSent);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
            _disconnected.Add(e.Session);
    }

    private void ClearSendHistory(PvsSession session)
    {
        if (session.Overflow != null)
            _entDataListPool.Return(session.Overflow.Value.SentEnts);
        session.Overflow = null;

        foreach (var visSet in session.PreviouslySent.Values)
        {
            _entDataListPool.Return(visSet);
        }

        session.PreviouslySent.Clear();
        session.LastSent = null;
    }
}
