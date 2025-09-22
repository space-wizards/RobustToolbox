using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Prometheus;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
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
        if (!PlayerData.TryGetValue(session, out var sessionData))
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
    private WaitHandle? ProcessQueuedAcks()
    {
        if (PendingAcks.Count == 0)
            return null;

        _toAck.Clear();

        foreach (var session in PendingAcks)
        {
            if (session.Status != SessionStatus.Disconnected)
                _toAck.Add(GetOrNewPvsSession(session));
        }

        PendingAcks.Clear();

        if (!_async)
        {
            using var _= Histogram.WithLabels("Process Acks").NewTimer();
            _parallelManager.ProcessNow(_ackJob, _ackJob.Count);
            return null;
        }

        return _parallelManager.Process(_ackJob, _ackJob.Count);
    }

    private record struct PvsAckJob(PvsSystem _pvs) : IParallelRobustJob
    {
        public int BatchSize => 2;
        private PvsSystem _pvs = _pvs;
        public int Count => _pvs._toAck.Count;

        public void Execute(int index)
        {
            try
            {
                _pvs.ProcessQueuedAck(_pvs._toAck[index]);
            }
            catch (Exception e)
            {
                _pvs.Log.Log(LogLevel.Error, e, $"Caught exception while processing PVS acks.");
            }
        }
    }

    private record struct PvsChunkJob(PvsSystem _pvs) : IParallelRobustJob
    {
        public int BatchSize => 2;
        private PvsSystem _pvs = _pvs;
        public int Count => _pvs._dirtyChunks.Count;

        public void Execute(int index)
        {
            try
            {
                _pvs.UpdateDirtyChunks(index);
            }
            catch (Exception e)
            {
                _pvs.Log.Log(LogLevel.Error, e, $"Caught exception while updating dirty PVS chunks.");
            }
        }
    }

    /// <summary>
    ///     Process a given client's queued ack.
    /// </summary>
    private void ProcessQueuedAck(PvsSession session)
    {
        var ackedTick = session.LastReceivedAck;
        List<PvsIndex>? ackedEnts;

        if (session.Overflow != null && session.Overflow.Value.Tick <= ackedTick)
        {
            var (overflowTick, overflowEnts) = session.Overflow.Value;
            session.Overflow = null;
            ackedEnts = overflowEnts;

            // Even though the acked tick might be newer, we have no guarantee that the client received the cached tick,
            // so discard it unless they happen to be equal.
            if (overflowTick != ackedTick)
            {
                _entDataListPool.Return(overflowEnts);
                DebugTools.Assert(!session.PreviouslySent.Values.Contains(overflowEnts));
                return;
            }
        }
        else if (!session.PreviouslySent.TryGetValue(ackedTick, out ackedEnts))
            return;

        foreach (ref var intPtr in CollectionsMarshal.AsSpan(ackedEnts))
        {
            ref var data = ref session.DataMemory.GetRef(intPtr.Index);
            DebugTools.AssertNotEqual(data.LastSeen, GameTick.Zero);
            DebugTools.Assert(data.LastSeen >= ackedTick); // LastSent may equal ackedTick if the packet was sent reliably.
            data.EntityLastAcked = ackedTick;
        }

        // The client acked a tick. If they requested a full state, this ack happened some time after that, so we can safely set this to false
        session.RequestedFull = false;
    }
}
