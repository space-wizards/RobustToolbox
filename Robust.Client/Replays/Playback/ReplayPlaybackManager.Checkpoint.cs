using System;
using System.Linq;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Shared.GameObjects;
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
    public void ResetToNearestCheckpoint(int index, bool flushEntities, CheckpointState? checkpoint = null)
    {
        if (Replay == null)
            throw new Exception("Not currently playing a replay");

        if (flushEntities)
            _entMan.FlushEntities();

        // Look up the desired checkpoint, unless our caller kindly provided one to us.
        checkpoint ??= GetLastCheckpoint(Replay, index);

        _sawmill.Info($"Resetting to checkpoint. From {Replay.CurrentIndex} to {checkpoint.Index}");
        var st = new Stopwatch();
        st.Start();

        Replay.CurrentIndex = checkpoint.Index;
        DebugTools.Assert(Replay.ClientSideRecording
                          || checkpoint.Tick == new GameTick(Replay.TickOffset.Value + (uint) Replay.CurrentIndex));

        foreach (var (name, value) in checkpoint.Cvars)
        {
            _netConf.SetCVar(name, value, force: true);
        }

        _timing.TimeBase = checkpoint.TimeBase;
        _timing.CurTick = _timing.LastRealTick = _timing.LastProcessedTick = new GameTick(Replay.TickOffset.Value + (uint) Replay.CurrentIndex);
        Replay.LastApplied = checkpoint.Tick;

        ApplyCheckpointState(checkpoint, Replay);

        ReplayCheckpointReset?.Invoke();

        _sawmill.Info($"Resetting to checkpoint took {st.Elapsed}");
        StopAudio();
        _timing.CurTick += 1;
    }

    private void ApplyCheckpointState(CheckpointState checkpoint, ReplayData replay)
    {
        DebugTools.Assert(replay.ClientSideRecording || checkpoint.Detached.Count == 0);

        var nextIndex = checkpoint.Index + 1;
        var next =  nextIndex < replay.States.Count ? replay.States[nextIndex] : null;
        _gameState.PartialStateReset(checkpoint.FullState, false, false);
        _entMan.EntitySysManager.GetEntitySystem<ClientDirtySystem>().Reset();
        _entMan.EntitySysManager.GetEntitySystem<TransformSystem>().Reset();
        _gameState.UpdateFullRep(checkpoint.FullState, cloneDelta: true);
        _gameState.ClearDetachQueue();
        EnsureDetachedExist(checkpoint);
        _gameState.DetachImmediate(checkpoint.Detached);
        BeforeApplyState?.Invoke((checkpoint.State, next));
        _gameState.ApplyGameState(checkpoint.State, next);
    }

    private void EnsureDetachedExist(CheckpointState checkpoint)
    {
        // Client-side replays only apply states for currently attached entities. But this means that when rewinding
        // time we need to ensure that detached entities still get "un-deleted".
        // Also important when jumping forward to a point after the entity was first encountered and then detached.

        if (checkpoint.DetachedStates == null)
            return;

        DebugTools.Assert(checkpoint.Detached.Count == checkpoint.DetachedStates.Length);
        foreach (var es in checkpoint.DetachedStates)
        {
            if (_entMan.TryGetEntityData(es.NetEntity, out var uid, out var meta))
            {
                DebugTools.Assert(!meta.EntityDeleted);
                continue;
            }

            var metaState = (MetaDataComponentState?)es.ComponentChanges.Value?
                .FirstOrDefault(c => c.NetID == _metaId).State;

            if (metaState == null)
                throw new MissingMetadataException(es.NetEntity);

            uid = _entMan.CreateEntity(metaState.PrototypeId, out meta);

            // Client creates a client-side net entity for the newly created entity.
            // We need to clear this mapping before assigning the real net id.
            // TODO NetEntity Jank: prevent the client from creating this in the first place.
            _entMan.ClearNetEntity(meta.NetEntity);
            _entMan.SetNetEntity(uid.Value, es.NetEntity, meta);

            _entMan.InitializeEntity(uid.Value, meta);
            _entMan.StartEntity(uid.Value);
            meta.LastStateApplied = checkpoint.Tick;
        }
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
