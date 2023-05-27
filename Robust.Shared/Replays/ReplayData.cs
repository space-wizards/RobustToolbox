using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Robust.Shared.Replays;

/// <summary>
///     This class contains data read from some replay recording.
/// </summary>
public sealed class ReplayData
{
    /// <summary>
    /// List of game states for each tick.
    /// </summary>
    public readonly List<GameState> States;

    /// <summary>
    /// List of all networked messages and variables that were sent each tick.
    /// </summary>
    public readonly List<ReplayMessage> Messages;

    /// <summary>
    /// The first tick in this recording.
    /// </summary>
    public readonly GameTick TickOffset;

    /// <summary>
    /// The sever's time when the recording was started.
    /// </summary>
    public readonly TimeSpan StartTime;

    /// <summary>
    /// The length of this recording.
    /// </summary>
    public readonly TimeSpan Duration;

    /// <summary>
    /// Array of checkpoint states. These are full game states that make it faster to jump around in time.
    /// </summary>
    public readonly CheckpointState[] Checkpoints;

    /// <summary>
    /// This indexes the <see cref="States"/> and <see cref="Messages"/> lists. It is basically the "current tick"
    /// but without the <see cref="TickOffset"/> .
    /// </summary>
    public int CurrentIndex;

    public GameTick LastApplied;


    public GameTick CurTick => new GameTick((uint) CurrentIndex + TickOffset.Value);
    public GameState CurState => States[CurrentIndex];
    public GameState? NextState => CurrentIndex + 1 < States.Count ? States[CurrentIndex + 1] : null;
    public ReplayMessage CurMessages => Messages[CurrentIndex];

    /// <summary>
    /// The initial set of messages that were added to the recording before any tick was ever recorded. This might
    /// contain data required to properly parse the rest of the recording (e.g., prototype uploads)
    /// </summary>
    public ReplayMessage? InitialMessages;

    public ReplayData(List<GameState> states,
        List<ReplayMessage> messages,
        GameTick tickOffset,
        TimeSpan startTime,
        TimeSpan duration,
        CheckpointState[] checkpointStates,
        ReplayMessage? initData)
    {
        States = states;
        Messages = messages;
        TickOffset = tickOffset;
        StartTime = startTime;
        Duration = duration;
        Checkpoints = checkpointStates;
        InitialMessages = initData;
    }
}


/// <summary>
/// Checkpoints are full game states that make it faster to jump around in time. I.e., instead of having to apply 1000
/// game states to get from tick 1 to 1001, you can jump directly to the nearest checkpoint and apply much fewer states.
/// </summary>
public readonly struct CheckpointState : IComparable<CheckpointState>
{
    public GameTick Tick => State.ToSequence;
    public readonly GameState State;
    public readonly (TimeSpan, GameTick) TimeBase;
    public readonly int Index;
    public readonly Dictionary<string, object> Cvars;

    public CheckpointState(GameState state, (TimeSpan, GameTick) time, Dictionary<string, object> cvars, int index)
    {
        State = state;
        TimeBase = time;
        Cvars = cvars.ShallowClone();
        Index = index;
    }

    /// <summary>
    ///     Get a dummy state for use with bisection searches.
    /// </summary>
    public static CheckpointState DummyState(int index)
    {
        return new CheckpointState(index);
    }

    private CheckpointState(int index)
    {
        Index = index;
        State = default!;
        TimeBase = default!;
        Cvars = default!;
    }

    public int CompareTo(CheckpointState other) => Index.CompareTo(other.Index);
}

/// <summary>
/// Collection of all networked messages and variables that were sent in a given tick.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReplayMessage
{
    public List<object> Messages = default!;

    [Serializable, NetSerializable]
    public sealed class CvarChangeMsg
    {
        public List<(string name, object value)> ReplicatedCvars = default!;
        public (TimeSpan, GameTick) TimeBase = default;
    }
}
