using System;
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
        _sendTick = _gameTiming.CurTick;

        if (_async)
        {
            _sendTask = _parallelManager.Process(_sendJob, _sendJob.Count);
            return;
        }

        using var _ = Histogram.WithLabels("Send States").NewTimer();
        _parallelManager.ProcessNow(_sendJob, _sendJob.Count);
    }

    private void SendSessionState(PvsSession data, ZStdCompressionContext ctx, GameTick sendTick)
    {
        // Ensure it gets disposed if any exceptions happen.
        using var stateStream = data.StateStream;
        data.StateStream = null;

        // PVS benchmarks use dummy sessions.
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (data.Session.Channel is not DummyChannel)
        {
            if (!data.Session.Channel.IsConnected)
                return;

            DebugTools.AssertNotEqual(stateStream, null);
            var msg = new MsgState
            {
                StateStream = stateStream,
                ForceSendReliably = data.ForceSendReliably,
                CompressionContext = ctx
            };

            _netMan.ServerSendMessage(msg, data.Session.Channel);
            // The channel can disconnect between our check and the network manager's check.
            if (msg.HasWritten && msg.ShouldSendReliably())
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
    }

    private record struct PvsSendJob(PvsSystem _pvs) : IParallelRobustJob
    {
        public int BatchSize => 1;
        private PvsSystem _pvs = _pvs;
        public int Count => _pvs._sessions.Length;

        public void Execute(int index)
        {
            var data = _pvs._sessions[index];
            var resource = _pvs._threadResourcesPool.Get();

            try
            {
                _pvs.SendSessionState(data, resource.CompressionContext, _pvs._sendTick);
            }
            catch (Exception e)
            {
                _pvs.Log.Log(LogLevel.Error, e, $"Caught exception while sending mail for {data.Session}.");
#if !EXCEPTION_TOLERANCE
                throw;
#endif
            }
            finally
            {
                _pvs._threadResourcesPool.Return(resource);
            }
        }
    }
}
