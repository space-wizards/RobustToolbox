using System;
using System.Threading.Tasks;
using Prometheus;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

internal sealed partial class PvsSystem
{
    [Dependency] private readonly IRobustSerializer _serializer = default!;

    /// <summary>
    /// Get and serialize <see cref="GameState"/> objects for each player. Compressing & sending the states is done later.
    /// </summary>
    private void SerializeStates()
    {
        using var _ = Histogram.WithLabels("Serialize States").NewTimer();
        var opts = new ParallelOptions {MaxDegreeOfParallelism = _parallelMgr.ParallelProcessCount};
        _oldestAck = GameTick.MaxValue.Value;
        Parallel.For(-1, _sessions.Length, opts, SerializeState);
    }

    /// <summary>
    /// Get and serialize a <see cref="GameState"/> for a single session (or the current replay).
    /// </summary>
    private void SerializeState(int i)
    {
        try
        {
            var guid = i >= 0 ? _sessions[i].Session.UserId.UserId : default;
            ServerGameStateManager.PvsEventSource.Log.WorkStart(_gameTiming.CurTick.Value, i, guid);

            if (i >= 0)
                SerializeSessionState(_sessions[i]);
            else
                _replay.Update();

            ServerGameStateManager.PvsEventSource.Log.WorkStop(_gameTiming.CurTick.Value, i, guid);
        }
        catch (Exception e) // Catch EVERY exception
        {
            var source = i >= 0 ? _sessions[i].Session.ToString() : "replays";
            Log.Log(LogLevel.Error, e, $"Caught exception while serializing game state for {source}.");
        }
    }

    /// <summary>
    /// Get and serialize a <see cref="GameState"/> for a single session.
    /// </summary>
    private void SerializeSessionState(PvsSession data)
    {
        ComputeSessionState(data);
        InterlockedHelper.Min(ref _oldestAck, data.FromTick.Value);
        DebugTools.AssertEqual(data.StateStream, null);

        // PVS benchmarks use dummy sessions.
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (data.Session.Channel is not DummyChannel)
        {
            data.StateStream = RobustMemoryManager.GetMemoryStream();
            _serializer.SerializeDirect(data.StateStream, data.State);
        }

        data.ClearState();
    }
}
