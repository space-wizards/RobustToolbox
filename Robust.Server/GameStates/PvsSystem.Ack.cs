using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Prometheus;
using Robust.Shared.Player;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

// This partial class contains code relating to acknowledging game states received by clients.
internal sealed partial class PvsSystem
{
    /// <summary>
    ///     Invoked when a client ack message is received. Queues up for processing in parallel prior to sending game
    ///     state data.
    /// </summary>
    private void OnClientAck(ICommonSession session, GameTick ackedTick)
    {
        DebugTools.Assert(ackedTick < _gameTiming.CurTick);
        if (!_playerData.TryGetValue(session, out var sessionData))
            return;

        if (ackedTick <= sessionData.LastReceivedAck)
            return;

        sessionData.LastReceivedAck = ackedTick;
        PendingAcks.Add(session);
    }

    /// <summary>
    ///     Processes queued client acks in parallel
    /// </summary>
    /// <param name="histogram"></param>
    internal WaitHandle? ProcessQueuedAcks(Histogram? histogram)
    {
        if (PendingAcks.Count == 0)
            return null;

        _toAck.Clear();

        foreach (var session in PendingAcks)
        {
            _toAck.Add(session);
        }

        PendingAcks.Clear();

        if (!_async)
        {
            using var _= histogram?.WithLabels("Process Acks").NewTimer();
            _parallelManager.ProcessNow(_ackJob, _toAck.Count);
            return null;
        }

        return _parallelManager.Process(_ackJob, _toAck.Count);
    }

    private record struct PvsAckJob : IParallelRobustJob
    {
        public int BatchSize => 2;

        public PvsSystem System;
        public List<ICommonSession> Sessions;

        public void Execute(int index)
        {
            System.ProcessQueuedAck(Sessions[index]);
        }
    }

    private record struct PvsChunkJob : IParallelRobustJob
    {
        public int BatchSize => 2;
        public PvsSystem Pvs;
        public int Count => Pvs._dirtyChunks.Count + 2;

        public void Execute(int index)
        {
            if (index > 1)
            {
                Pvs.UpdateDirtyChunks(index-2);
                return;
            }

            // 1st batch/job performs some extra processing.
            if (index == 0)
                Pvs.CacheGlobalOverrides();
            else if (index == 1)
                Pvs.UpdateCleanChunks();
        }
    }

    /// <summary>
    ///     Process a given client's queued ack.
    /// </summary>
    private void ProcessQueuedAck(ICommonSession session)
    {
        if (!_playerData.TryGetValue(session, out var sessionData))
            return;

        var ackedTick = sessionData.LastReceivedAck;
        List<EntityData>? ackedEnts;

        if (sessionData.Overflow != null && sessionData.Overflow.Value.Tick <= ackedTick)
        {
            var (overflowTick, overflowEnts) = sessionData.Overflow.Value;
            sessionData.Overflow = null;
            ackedEnts = overflowEnts;

            // Even though the acked tick might be newer, we have no guarantee that the client received the cached tick,
            // so discard it unless they happen to be equal.
            if (overflowTick != ackedTick)
            {
                _entDataListPool.Return(overflowEnts);
                DebugTools.Assert(!sessionData.PreviouslySent.Values.Contains(overflowEnts));
                return;
            }
        }
        else if (!sessionData.PreviouslySent.TryGetValue(ackedTick, out ackedEnts))
            return;

        foreach (var data in CollectionsMarshal.AsSpan(ackedEnts))
        {
            data.EntityLastAcked = ackedTick;
            DebugTools.Assert(data.Visibility > PvsEntityVisibility.Unsent);
            DebugTools.Assert(data.LastSeen >= ackedTick); // LastSent may equal ackedTick if the packet was sent reliably.
            DebugTools.Assert(!sessionData.Entities.TryGetValue(data.NetEntity, out var old)
                              || ReferenceEquals(data, old));
        }

        // The client acked a tick. If they requested a full state, this ack happened some time after that, so we can safely set this to false
        sessionData.RequestedFull = false;
    }
}
