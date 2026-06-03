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
    [Dependency] private IRobustSerializer _serializer = default!;

    /// <summary>
    /// Get and serialize <see cref="GameState"/> objects for each player. Compressing & sending the states is done later.
    /// </summary>
    private void SerializeStates()
    {
        using var _ = Histogram.WithLabels("Serialize States").NewTimer();
        var opts = new ParallelOptions {MaxDegreeOfParallelism = _parallelMgr.ParallelProcessCount};
        _oldestAck = GameTick.MaxValue.Value;

        // When state reuse is on, split into two parallel passes: pass 1 builds every session's GameState
        // (populating the per-component reuse cache and pre-serializing shared component bytes), pass 2 does
        // the monolithic SerializeDirect. Splitting avoids a thundering herd of sessions racing to compute
        // the same shared state, and keeps each shared state computed and serialized exactly once. When reuse
        // is off, use the original combined pass.
        if (_stateReuse)
        {
            Parallel.For(-1, _sessions.Length, opts, ComputeState);
            Parallel.For(-1, _sessions.Length, opts, WriteState);
            return;
        }

        Parallel.For(-1, _sessions.Length, opts, SerializeState);
    }

    /// <summary>
    /// First reuse pass: build a session's <see cref="GameState"/> (or run the replay, which does its own
    /// compute and serialize in one shot and is therefore handled entirely here).
    /// </summary>
    private void ComputeState(int i)
    {
        try
        {
            if (i < 0)
            {
                _replay.Update();
                return;
            }

            var data = _sessions[i];
            using (Histogram.WithLabels("Reuse Build").NewTimer())
                ComputeSessionState(data);
            InterlockedHelper.Min(ref _oldestAck, data.FromTick.Value);
        }
        catch (Exception e)
        {
            Log.Log(LogLevel.Error, e, $"Caught exception while computing game state for {(i >= 0 ? _sessions[i].Session.ToString() : "replays")}.");
#if !EXCEPTION_TOLERANCE
            throw;
#endif
        }
    }

    /// <summary>
    /// Second reuse pass: serialize a session's already-built <see cref="GameState"/> into its stream.
    /// </summary>
    private void WriteState(int i)
    {
        try
        {
            if (i < 0)
                return; // Replay was fully handled in the compute pass.

            var data = _sessions[i];
            DebugTools.AssertEqual(data.StateStream, null);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (data.Session.Channel is not DummyChannel)
            {
                data.StateStream = RobustMemoryManager.GetMemoryStream();
                using (Histogram.WithLabels("Reuse Write").NewTimer())
                    _serializer.SerializeDirect(data.StateStream, data.State);
            }
            data.ClearState();
        }
        catch (Exception e)
        {
            Log.Log(LogLevel.Error, e, $"Caught exception while serializing game state for {(i >= 0 ? _sessions[i].Session.ToString() : "replays")}.");
#if !EXCEPTION_TOLERANCE
            throw;
#endif
        }
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
#if !EXCEPTION_TOLERANCE
            throw;
#endif
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
