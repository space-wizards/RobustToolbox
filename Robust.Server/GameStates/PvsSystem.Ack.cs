using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Robust.Shared.GameObjects;
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
    internal WaitHandle ProcessQueuedAcks()
    {
        if (PendingAcks.Count == 0)
        {
            return ParallelManager.DummyResetEvent.WaitHandle;
        }

        _toAck.Clear();

        foreach (var session in PendingAcks)
        {
            _toAck.Add(session);
        }

        PendingAcks.Clear();
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

    /// <summary>
    ///     Process a given client's queued ack.
    /// </summary>
    private void ProcessQueuedAck(ICommonSession session)
    {
        if (!PlayerData.TryGetValue(session, out var sessionData))
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
                DebugTools.Assert(!sessionData.SentEntities.Values.Contains(overflowEnts));
                return;
            }
        }
        else if (!sessionData.SentEntities.TryGetValue(ackedTick, out ackedEnts))
            return;

        var entityData = sessionData.EntityData;
        foreach (var data in CollectionsMarshal.AsSpan(ackedEnts))
        {
            data.EntityLastAcked = ackedTick;
            DebugTools.Assert(data.LastSent >= ackedTick); // LastSent may equal ackedTick if the packet was sent reliably.
        }

        // The client acked a tick. If they requested a full state, this ack happened some time after that, so we can safely set this to false
        sessionData.RequestedFull = false;
    }
}
