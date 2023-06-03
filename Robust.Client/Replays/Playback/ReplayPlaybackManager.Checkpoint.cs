using System;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.Replays.Playback;

// This partial class contains codes for modifying the current game tick/time.
internal sealed partial class ReplayPlaybackManager
{
    /// <summary>
    /// This function resets the game state to some checkpoint state. This is effectively what enables rewinding time.
    /// </summary>
    /// <param name="index">The target tick/index. The actual checkpoint will have an index less than or equal to this.</param>
    /// <param name="flushEntities">Whether to delete all entities</param>
    public void ResetToNearestCheckpoint(int index, bool flushEntities)
    {
        if (Replay == null)
            throw new Exception("Not currently playing a replay");

        if (flushEntities)
            _entMan.FlushEntities();

        var checkpoint = GetLastCheckpoint(Replay, index);
        var state = checkpoint.State;

        _sawmill.Info($"Resetting to checkpoint. From {Replay.CurrentIndex} to {checkpoint.Index}");
        var st = new Stopwatch();
        st.Start();

        Replay.CurrentIndex = checkpoint.Index;
        DebugTools.Assert(state.ToSequence == new GameTick(Replay.TickOffset.Value + (uint) Replay.CurrentIndex));

        foreach (var (name, value) in checkpoint.Cvars)
        {
            _netConf.SetCVar(name, value, force: true);
        }

        _timing.TimeBase = checkpoint.TimeBase;
        _timing.CurTick = _timing.LastRealTick = _timing.LastProcessedTick = new GameTick(Replay.TickOffset.Value + (uint) Replay.CurrentIndex);
        Replay.LastApplied = state.ToSequence;

        _gameState.PartialStateReset(state, false, false);
        _entMan.EntitySysManager.GetEntitySystem<ClientDirtySystem>().Reset();
        _entMan.EntitySysManager.GetEntitySystem<TransformSystem>().Reset();

        _gameState.UpdateFullRep(state, cloneDelta: true);
        _gameState.ApplyGameState(state, Replay.NextState);

        ReplayCheckpointReset?.Invoke();

        _sawmill.Info($"Resetting to checkpoint took {st.Elapsed}");
        StopAudio();
        _timing.CurTick += 1;
    }

    public CheckpointState GetLastCheckpoint(ReplayData data, int index)
    {
        var target = CheckpointState.DummyState(index);
        var checkpointIndex = Array.BinarySearch(data.Checkpoints, target);

        if (checkpointIndex < 0)
            checkpointIndex = Math.Max(0, ~checkpointIndex - 1);

        var checkpoint = data.Checkpoints[checkpointIndex];
        DebugTools.Assert(checkpoint.Index <= index);
        DebugTools.Assert(checkpointIndex == data.Checkpoints.Length - 1 || data.Checkpoints[checkpointIndex + 1].Index > index);
        return checkpoint;
    }

    public CheckpointState GetNextCheckpoint(ReplayData data, int index)
    {
        var target = CheckpointState.DummyState(index);
        var checkpointIndex = Array.BinarySearch(data.Checkpoints, target);

        if (checkpointIndex < 0)
            checkpointIndex = Math.Max(0, ~checkpointIndex - 1);

        checkpointIndex = Math.Clamp(checkpointIndex + 1, 0, data.Checkpoints.Length - 1);

        var checkpoint = data.Checkpoints[checkpointIndex];
        DebugTools.Assert(checkpoint.Index >= index || checkpointIndex == data.Checkpoints.Length - 1);
        return checkpoint;
    }
}
