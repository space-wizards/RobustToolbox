using System;
using System.Threading.Tasks;
using Prometheus;
using Robust.Shared.Log;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

internal sealed partial class PvsSystem
{
    /// <summary>
    /// Compress and send game states to connected clients.
    /// </summary>
    private void SendStates()
    {
        // TODO PVS make this async
        // AFAICT ForEachAsync doesn't support using a threadlocal PvsThreadResources.
        // Though if it is getting pooled, does it really matter?

        // If this does get run async, then ProcessDisconnections() has to ensure that the job has finished before modifying
        // the sessions array

        using var _ = Histogram.WithLabels("Send States").NewTimer();
        var opts = new ParallelOptions {MaxDegreeOfParallelism = _parallelMgr.ParallelProcessCount};
        Parallel.ForEach(_sessions, opts, _threadResourcesPool.Get, SendSessionState, _threadResourcesPool.Return);
    }

    private PvsThreadResources SendSessionState(PvsSession data, ParallelLoopState state, PvsThreadResources resource)
    {
        try
        {
            SendSessionState(data, resource.CompressionContext);
        }
        catch (Exception e)
        {
            Log.Log(LogLevel.Error, e, $"Caught exception while sending mail for {data.Session}.");
        }

        return resource;
    }

    private void SendSessionState(PvsSession data, ZStdCompressionContext ctx)
    {
        DebugTools.AssertEqual(data.State, null);

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
                data.LastReceivedAck = _gameTiming.CurTick;
                lock (PendingAcks)
                {
                    PendingAcks.Add(data.Session);
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
                PendingAcks.Add(data.Session);
            }
        }

        data.StateStream?.Dispose();
        data.StateStream = null;
    }
}
