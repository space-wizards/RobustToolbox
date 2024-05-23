using System;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Shared.Utility;

namespace Robust.Client.Replays.Playback;

// This partial class contains codes for modifying the current game tick/time.
internal sealed partial class ReplayPlaybackManager
{
    public event Action? ReplayCheckpointReset;
    public event Action? BeforeSetTick;
    public event Action? AfterSetTick;

    public void SetIndex(int value, bool pausePlayback = true)
    {
        if (Replay == null)
            throw new Exception("Not currently playing a replay");

        if (_timing.ApplyingState)
        {
            // This fixes a niche error. If scrubbing forward in time, and the currently spectated entity gets deleted
            // this can trigger events that cause the player to be attached to a new entity. This may cause game UI
            // state / screen changes, which then trigger key-up events, which in turn cause scrubbing to end, thus
            // causing this method to try apply a game state while already in the middle of applying another state. So
            // we will just do nothing instead.
            return;
        }


        Playing &= !pausePlayback;
        value = Math.Clamp(value, 0, Replay.States.Count - 1);
        if (value == Replay.CurrentIndex)
            return;

        BeforeSetTick?.Invoke();

        bool skipEffectEvents = value > Replay.CurrentIndex + _visualEventThreshold;
        if (value < Replay.CurrentIndex)
        {
            skipEffectEvents = true;
            ResetToNearestCheckpoint(value, false);
        }
        else if (value > Replay.CurrentIndex + _checkpointMinInterval)
        {
            // If we are skipping many ticks into the future, we try to skip directly to a checkpoint instead of
            // applying every tick.
            var nextCheckpoint = GetNextCheckpoint(Replay, Replay.CurrentIndex);
            if (nextCheckpoint.Index < value && nextCheckpoint.Index > Replay.CurrentIndex)
                ResetToNearestCheckpoint(value, false);
        }

        _entMan.EntitySysManager.GetEntitySystem<ClientDirtySystem>().Reset();

        while (Replay.CurrentIndex < value)
        {
            Replay.CurrentIndex++;
            var state = Replay.CurState;

            _timing.LastRealTick = _timing.LastProcessedTick = _timing.CurTick = Replay.CurTick;
            _gameState.UpdateFullRep(state, cloneDelta: true);

            // Clear existing lerps
            _entMan.EntitySysManager.GetEntitySystem<TransformSystem>().Reset();

            var next = Replay.NextState;
            BeforeApplyState?.Invoke((state, next));
            _gameState.ApplyGameState(state, next);
            ProcessMessages(Replay.CurMessages, skipEffectEvents);

            // TODO REPLAYS block audio
            // Just block audio/midi from ever starting, rather than repeatedly stopping it.
            StopAudio();
;
            DebugTools.Assert(Replay.LastApplied >= state.FromSequence);
            DebugTools.Assert(Replay.LastApplied + 1 <= state.ToSequence);
            Replay.LastApplied = state.ToSequence;
        }

        AfterSetTick?.Invoke();
    }

    public int GetIndex(TimeSpan time)
    {
        if (Replay == null)
            throw new Exception("Not currently playing a replay");

        if (time <= TimeSpan.Zero)
            return 0;

        if (time >= Replay.ReplayTime[^1])
            return Replay.States.Count - 1;

        var index = Array.BinarySearch(Replay.ReplayTime, time);

        if (index < 0)
            index = Math.Max(0, ~index - 1);

        return index;
    }
}
