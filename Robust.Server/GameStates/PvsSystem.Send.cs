using System;
using System.Threading;
using Microsoft.Extensions.ObjectPool;
using Prometheus;
using Robust.Shared.Log;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

internal sealed partial class PvsSystem
{
    /// <summary>
    /// Compress and send game states to connected clients.
    /// </summary>
    private void SendStates()
    {
        DebugTools.AssertNull(_sendTask);
        // CVar changes may replace the pool while workers are still running so copy it every time.
        var job = new PvsSendJob(this, _sessions, _threadResourcesPool, _gameTiming.CurTick);

        if (_async)
        {
            _sendTask = _parallelManager.Process(job, job.Count);
            return;
        }

        _parallelManager.ProcessNow(job, job.Count);
    }

    private void SendSessionState(PvsSession data, ZStdCompressionContext ctx, GameTick sendTick)
    {
        // PVS benchmarks use dummy sessions.
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (data.Session.Channel is not DummyChannel)
        {
            DebugTools.AssertNotEqual(data.StateStream, null);
            var msg = new MsgState
            {
                StateStream = data.StateStream,
                ForceSendReliably = data.ForceSendReliably,
                CompressionContext = ctx
            };

            _netMan.ServerSendMessage(msg, data.Session.Channel);
            if (msg.ShouldSendReliably())
            {
                data.RequestedFull = false;
                data.LastReceivedAck = sendTick;
                lock (PendingAcks)
                {
                    PendingAcks.Add(data.Session);
                }
            }
        }
        else
        {
            // Always "ack" dummy sessions.
            data.LastReceivedAck = sendTick;
            data.RequestedFull = false;
            lock (PendingAcks)
            {
                PendingAcks.Add(data.Session);
            }
        }

        data.StateStream?.Dispose();
        data.StateStream = null;
    }

    private sealed class PvsSendJob : IParallelRobustJob
    {
        private readonly PvsSystem _pvs;
        private readonly PvsSession[] _sessions;
        private readonly DefaultObjectPool<PvsThreadResources> _pool;
        private readonly GameTick _sendTick;
        private readonly IDisposable _timer;
        private int _remaining;

        public PvsSendJob(PvsSystem pvs, PvsSession[] sessions,
            DefaultObjectPool<PvsThreadResources> pool, GameTick sendTick)
        {
            _pvs = pvs;
            _sessions = sessions;
            _pool = pool;
            _sendTick = sendTick;
            _remaining = sessions.Length;
            _timer = Histogram.WithLabels("Send States").NewTimer();
            if (_remaining == 0)
                _timer.Dispose();
        }

        public int BatchSize => 1;
        public int Count => _sessions.Length;

        public void Execute(int index)
        {
            try
            {
                var data = _sessions[index];
                var resource = _pool.Get();
                try
                {
                    _pvs.SendSessionState(data, resource.CompressionContext, _sendTick);
                }
                catch (Exception e)
                {
                    _pvs.Log.Log(LogLevel.Error, e, $"Caught exception while sending mail for {data.Session}.");
                }
                finally
                {
                    _pool.Return(resource);
                }
            }
            finally
            {
                // Measure for grafana when they're all done.
                if (Interlocked.Decrement(ref _remaining) == 0)
                    _timer.Dispose();
            }
        }
    }
}
