using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Markdown.Mapping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Robust.Shared.ContentPack;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Robust.Shared.Replays;

public interface IReplayRecordingManager
{
    /// <summary>
    /// Initializes the replay manager.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Whether or not a replay recording can currently be started.
    /// </summary>
    bool CanStartRecording();

    /// <summary>
    /// This is a convenience variation of <see cref="RecordReplayMessage"/> that only records messages for server-side
    /// recordings.
    /// </summary>
    void RecordServerMessage(object obj);

    /// <summary>
    /// This is a convenience variation of <see cref="RecordReplayMessage"/> that only records messages for client-side
    /// recordings.
    /// </summary>
    void RecordClientMessage(object obj);

    /// <summary>
    /// Queues some net-serializable data to be saved by a replay recording. Does nothing if <see cref="IsRecording"/>
    /// is false.
    /// </summary>
    /// <remarks>
    /// The queued object is typically something like an <see cref="EntityEventArgs"/>, so that replays can
    /// simulate receiving networked messages. However, this can really be any serializable data and could be used
    /// for saving server-exclusive data like power net or atmos pipe-net data for replaying. Alternatively, those
    /// could also just use networked component states on entities that are in null space and hidden from all
    /// players (but still saved to replays).
    /// </remarks>
    void RecordReplayMessage(object obj);

    /// <summary>
    ///     Whether the server is currently recording replay data.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Processes pending write tasks and saves the replay data for the current tick. This should be called even if a
    /// replay is not currently being recorded.
    /// </summary>
    void Update(GameState? state);

    /// <summary>
    /// This gets invoked whenever a replay recording is starting. Subscribers can use this to add extra yaml data
    /// to the recording's metadata file, as well as to provide serializable messages that get replayed when the replay
    /// is initially loaded. E.g., this should contain networked events that would get sent to a newly connected client.
    /// </summary>
    event Action<MappingDataNode, List<object>>? RecordingStarted;

    /// <summary>
    /// This gets invoked whenever a replay recording is stopping. Subscribers can use this to add extra yaml data to the
    /// recording's metadata file.
    /// </summary>
    event Action<MappingDataNode>? RecordingStopped;

    /// <summary>
    /// This gets invoked after a replay recording has finished and provides information about where the replay data
    /// was saved. Note that this only means that all write tasks have started, however some of the file tasks may not
    /// have finished yet. See <see cref="WaitWriteTasks"/>.
    /// </summary>
    event Action<IWritableDirProvider, ResPath>? RecordingFinished;

    /// <summary>
    /// Tries to starts a replay recording.
    /// </summary>
    /// <param name="directory">
    /// The directory that the replay will be written to. E.g., <see cref="IResourceManager.UserData"/>.
    /// </param>
    /// <param name="name">
    /// The name of the replay. This will also determine the folder where the replay will be stored, which will be a
    /// subfolder within  <see cref="CVars.ReplayDirectory"/>. If not provided, will default to using the current time.
    /// </param>
    /// <param name="overwrite">
    /// Whether to overwrite the specified path if a folder already exists.
    /// </param>
    /// <param name="duration">
    /// Optional time limit for the recording.
    /// </param>
    /// <returns>Returns true if the recording was successfully started.</returns>
    bool TryStartRecording(
        IWritableDirProvider directory,
        string? name = null,
        bool overwrite = false,
        TimeSpan? duration = null);

    /// <summary>
    /// Stops an ongoing replay recording.
    /// </summary>
    void StopRecording();

    /// <summary>
    /// Returns information about the currently ongoing replay recording, including the currently elapsed time and the
    /// compressed replay size.
    /// </summary>
    (float Minutes, int Ticks, float Size, float UncompressedSize) GetReplayStats();

    /// <summary>
    /// Check the status of all async write tasks and return true if one of the tasks is still writing something.
    /// </summary>
    bool IsWriting();

    /// <summary>
    /// Returns a task that will wait for all the current writing tasks to finish.
    /// </summary>
    /// <exception cref="Exception">
    /// Throws an exception if <see cref="IsRecording"/> is true (i.e., new write tasks as still being created).
    /// </exception>
    Task WaitWriteTasks();

    // Define misc constants for writing and reading replays files.
    # region Constants

    /// <summary>
    /// File extension for data files that have to be deserialized and decompressed.
    /// </summary>
    public const string Ext = "dat";

    // filenames
    public static readonly ResPath MetaFile = new($"replay.yml");
    public static readonly ResPath CvarFile = new($"cvars.toml");
    public static readonly ResPath StringsFile = new($"strings.{Ext}");
    public static readonly ResPath InitFile = new($"init.{Ext}");

    // Yaml keys
    public const string Hash = "typeHash";
    public const string CompHash = "componentHash";
    public const string Strings = "stringHash";
    public const string Time = "time";
    public const string Name = "name";
    public const string Tick = "serverStartTime";
    public const string ServerTime = "startTick";
    public const string BaseTick = "timeBaseTick";
    public const string BaseTime = "timeBaseTimespan";
    public const string Duration = "duration";
    public const string Engine = "engineVersion";
    public const string Fork = "buildForkId";
    public const string ForkVersion = "buildVersion";
    public const string FileCount = "fileCount";
    public const string Compressed = "size";
    public const string Uncompressed = "uncompressedSize";
    public const string EndTick = "endTick";
    public const string EndTime = "serverEndTime";
    public const string IsClient = "clientRecording";
    public const string Recorder = "recordedBy";

    #endregion
}
