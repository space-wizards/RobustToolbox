using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using NetSerializer;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
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
    /// Replay recording time for each corresponding entry in <see cref="States"/>. Starts at 0.
    /// </summary>
    /// <remarks>
    /// This array exists mainly because the tickrate may change throughout a replay, so this makes it significantly
    /// easier to jump to some specific point in time.
    /// </remarks>
    public readonly TimeSpan[] ReplayTime;

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
    public readonly TimeSpan? Duration;

    /// <summary>
    /// Array of checkpoint states. These are full game states that make it faster to jump around in time.
    /// </summary>
    public readonly CheckpointState[] Checkpoints;

    /// <summary>
    /// This indexes the <see cref="States"/> and <see cref="Messages"/> lists. It is basically the "current tick"
    /// but without the <see cref="TickOffset"/> .
    /// </summary>
    /// <remarks>
    /// A negative value implies that the initial replay state has not yet been loaded (e.g., setting up cvars).
    /// </remarks>
    public int CurrentIndex { get; internal set; } = -1;

    public GameTick LastApplied { get; internal set; }

    public GameTick CurTick => new GameTick((uint) CurrentIndex + TickOffset.Value);
    public GameState CurState => States[CurrentIndex];
    public GameState? NextState => CurrentIndex + 1 < States.Count ? States[CurrentIndex + 1] : null;
    public ReplayMessage CurMessages => Messages[CurrentIndex];

    public TimeSpan CurrentReplayTime => ReplayTime[CurrentIndex];

    public readonly bool ClientSideRecording;
    public readonly MappingDataNode YamlData;

    /// <summary>
    /// If this is a client-side recording, this is the user that recorded that replay. Useful for setting default
    /// observer spawn positions.
    /// </summary>
    public readonly NetUserId? Recorder;

    /// <summary>
    /// The initial set of messages that were added to the recording before any tick was ever recorded. This might
    /// contain data required to properly parse the rest of the recording (e.g., prototype uploads)
    /// </summary>
    public ReplayMessage? InitialMessages;

    public ReplayData(List<GameState> states,
        List<ReplayMessage> messages,
        TimeSpan[] replayTime,
        GameTick tickOffset,
        TimeSpan startTime,
        TimeSpan? duration,
        CheckpointState[] checkpointStates,
        ReplayMessage? initData,
        bool clientSideRecording,
        MappingDataNode yamlData)
    {
        States = states;
        Messages = messages;
        ReplayTime = replayTime;
        TickOffset = tickOffset;
        StartTime = startTime;
        Duration = duration;
        Checkpoints = checkpointStates;
        InitialMessages = initData;
        ClientSideRecording = clientSideRecording;
        YamlData = yamlData;

        if (YamlData.TryGet(ReplayConstants.MetaKeyRecordedBy, out ValueDataNode? node)
            && Guid.TryParse(node.Value, out var guid))
        {
            Recorder = new NetUserId(guid);
        }
    }
}

/// <summary>
/// Checkpoints are full game states that make it faster to jump around in time. I.e., instead of having to apply 1000
/// game states to get from tick 1 to 1001, you can jump directly to the nearest checkpoint and apply much fewer states.
/// </summary>
public sealed class CheckpointState : IComparable<CheckpointState>
{
    public GameTick Tick => State.ToSequence;

    public readonly GameState FullState;

    public GameState State => AttachedStates ?? FullState;

    /// <summary>
    /// This is a variant of <see cref="FullState"/> for client-side replays that only contains information about entities
    /// not currently detached due to PVS range limits (see <see cref="Detached"/>).
    /// </summary>
    /// <remarks>
    /// This is required because we need <see cref="FullState"/> to update the full server state when jumping forward in
    /// time, but in general we do not want to apply the old-state from detached entities.
    ///
    /// To see why this is needed, consider a scenario where entity A parented to entity B. Then both leave PVS and
    /// ONLY entity B gets deleted. The client will not receive the new transform state for entity A, and if we blindly
    /// apply the full set of the most recent server states it will cause entity A to throw errors.
    /// </remarks>
    public readonly GameState? AttachedStates;

    public EntityState[]? DetachedStates;

    public readonly (TimeSpan, GameTick) TimeBase;
    public readonly int Index;
    public readonly Dictionary<string, object> Cvars;
    public readonly List<NetEntity> Detached;

    public CheckpointState(
        GameState state,
        (TimeSpan, GameTick) time,
        Dictionary<string, object> cvars,
        int index,
        HashSet<NetEntity> detached)
    {
        FullState = state;
        TimeBase = time;
        Cvars = cvars.ShallowClone();
        Index = index;
        Detached = new(detached);

        if (Detached.Count == 0)
            return;

        var attachedStates = new EntityState[state.EntityStates.Value.Count - Detached.Count];
        DetachedStates = new EntityState[Detached.Count];

        int i = 0, j = 0;
        foreach (var entState in state.EntityStates.Span)
        {
            if (detached.Contains(entState.NetEntity))
                DetachedStates[i++] = entState;
            else
                attachedStates[j++] = entState;

        }
        DebugTools.Assert(i == DetachedStates.Length);
        DebugTools.Assert(j == attachedStates.Length);

        AttachedStates = new GameState(
            state.FromSequence,
            state.ToSequence,
            state.LastProcessedInput,
            attachedStates,
            state.PlayerStates,
            state.EntityDeletions);
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
        FullState = default!;
        TimeBase = default!;
        Cvars = default!;
        Detached = default!;
        AttachedStates = default;
    }

    public int CompareTo(CheckpointState? other) => Index.CompareTo(other?.Index ?? -1);
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

    [Serializable, NetSerializable]
    public sealed class LeavePvs
    {
        public readonly List<NetEntity> Entities;
        public readonly GameTick Tick;

        public LeavePvs(List<NetEntity> entities, GameTick tick)
        {
            Entities = entities;
            Tick = tick;
        }
    }
}
