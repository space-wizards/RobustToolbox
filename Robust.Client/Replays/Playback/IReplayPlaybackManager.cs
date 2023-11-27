using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.Client.Replays.Playback;

public interface IReplayPlaybackManager
{
    public const string PlayCommand = "replay_play";
    public const string PauseCommand = "replay_pause";
    public const string ToggleCommand = "replay_toggle";
    public const string SkipCommand = "replay_skip";
    public const string SetCommand = "replay_set_time";
    public const string StopCommand = "replay_stop";
    public const string LoadCommand = "replay_load";

    void Initialize();

    /// <summary>
    /// Starts playing a replay.
    /// </summary>
    /// <param name="replay"></param>
    void StartReplay(ReplayData replay);

    /// <summary>
    /// Stops replaying a playback, unloads resources, and flushes all entities.
    /// </summary>
    void StopReplay();

    /// <summary>
    /// The replay that us currently being played back.
    /// </summary>
    ReplayData? Replay { get; }

    /// <summary>
    /// True if the replay is actively replaying ticks. False if the replay is currently paused.
    /// </summary>
    bool Playing { get; set; }

    /// <summary>
    /// This integer can be used by UI controls to scrub to some specific replay index.
    /// </summary>
    /// <remarks>
    /// This just automatically calls <see cref="SetIndex"/> every tick, as opposed to having the UI control call
    /// that method directly every frame update.
    /// </remarks>
    public int? ScrubbingTarget { get; set; }

    /// <summary>
    /// Set the current replay index (i.e., jump to a specific point in time). The zeroth index corresponds to the first
    /// tick of the replay.
    /// </summary>
    void SetIndex(int value, bool pausePlayback = true);

    /// <summary>
    /// Gets the index corresponding to some time. <see cref="TimeSpan.Zero"/> corresponds to the beginning
    /// of the replay.
    /// </summary>
    int GetIndex(TimeSpan time);

    /// <summary>
    /// Sets the current replay time to some specified value. <see cref="TimeSpan.Zero"/> corresponds to the beginning
    /// of the replay.
    /// </summary>
    void SetTime(TimeSpan time) => SetIndex(GetIndex(time));

    /// <summary>
    /// Invoked after replay playback has started and the first game state has been applied. Provides the replay
    /// metadata and the messages that were received just before the replay recording was started.
    /// </summary>
    event Action<MappingDataNode, List<object>>? ReplayPlaybackStarted;

    /// <summary>
    /// If not null, this will cause the playback to auto-pause after some number of ticks. E.g., if you want to advance
    /// the replay by 5 ticks and then pause, set this to 5 and set <see cref="Playing"/> to true.
    /// </summary>
    public uint? AutoPauseCountdown { get; set; }

    /// <summary>
    /// Invoked after replay playback has stopped and the replay has been unloaded.
    /// </summary>
    event Action? ReplayPlaybackStopped;

    /// <summary>
    /// Invoked when the replay rewinds or jumps forward to some checkpoint state.
    /// </summary>
    event Action? ReplayCheckpointReset;

    /// <summary>
    /// This gets invoked in order to allow content to handle a replay message. If the return value is false and the
    /// message is a <see cref="EntityEventArgs"/>, then it will simply be raised as if it had been received over the
    /// network.
    /// </summary>
    event HandleReplayMessageDelegate? HandleReplayMessage;

    /// <param name="message">The message that is to be handled</param>
    /// <param name="skipEffects">Whether transient/visual effects should be skipped. This option is true when skipping
    /// through large portions of the replay, in order to avoid spamming audio and other such effects while still
    /// applying important or game-state modifying messages.</param>
    delegate bool HandleReplayMessageDelegate(object message, bool skipEffects);

    /// <summary>
    /// This action is invoked just before jumping forward or backward in time. See also <see cref="AfterSetTick"/>.
    /// </summary>
    /// <remarks>
    /// This can be used by content to do things like fetching information about the current viewport / observer
    /// position, so that the position can be updated after the jump, which might potentially cause the old position to
    /// be invalid (e.g., map or grid might get deleted).
    /// </remarks>
    event Action? BeforeSetTick;

    /// <summary>
    /// This action is invoked after jumping forward or backward in time. See also <see cref="BeforeSetTick"/>.
    /// </summary>
    event Action? AfterSetTick;

    /// <summary>
    /// Invoked when the replay is paused.
    /// </summary>
    event Action? ReplayPaused;

    /// <summary>
    /// Invoked when the replay is unpaused.
    /// </summary>
    event Action? ReplayUnpaused;

    /// <summary>
    /// Invoked just before a replay applies a game state.
    /// </summary>
    event Action<(GameState Current, GameState? Next)>? BeforeApplyState;

    /// <summary>
    /// If currently replaying a client-side recording, this is the user that recorded the replay.
    /// Useful for setting default observer spawn positions.
    /// </summary>
    NetUserId? Recorder { get; }

    /// <summary>
    /// Fetches the entity that the <see cref="Recorder"/> is currently attached to.
    /// </summary>
    bool TryGetRecorderEntity([NotNullWhen(true)] out EntityUid? uid);
}
