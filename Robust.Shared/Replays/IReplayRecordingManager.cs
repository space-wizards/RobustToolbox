using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Markdown.Mapping;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;
using Robust.Shared.ContentPack;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Robust.Shared.Replays;

public interface IReplayRecordingManager
{
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
    /// Gets the <c>state</c> object passed into <see cref="TryStartRecording"/> for the current recording.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> if there is no active replay recording.
    /// </remarks>
    public object? ActiveRecordingState { get; }

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
    event Action<MappingDataNode, List<object>> RecordingStarted;

    /// <summary>
    /// This gets invoked whenever a replay recording is stopping. Subscribers can use this to add extra yaml data to the
    /// recording's metadata file.
    /// </summary>
    /// <seealso cref="RecordingStopped2"/>
    event Action<MappingDataNode> RecordingStopped;

    /// <summary>
    /// This gets invoked whenever a replay recording is stopping. Subscribers can use this to add extra data to the replay.
    /// </summary>
    /// <remarks>
    /// This is effectively a more powerful version of <see cref="RecordingStopped"/>.
    /// </remarks>
    event Action<ReplayRecordingStopped> RecordingStopped2;

    /// <summary>
    /// This gets invoked after a replay recording has finished and provides information about where the replay data
    /// was saved. Note that this only means that all write tasks have started, however some of the file tasks may not
    /// have finished yet. See <see cref="WaitWriteTasks"/>.
    /// </summary>
    event Action<ReplayRecordingFinished> RecordingFinished;

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
    /// <param name="state">
    /// An arbitrary object that is available in <see cref="ActiveRecordingState"/> and <see cref="RecordingFinished"/>.
    /// </param>
    /// <returns>Returns true if the recording was successfully started.</returns>
    bool TryStartRecording(
        IWritableDirProvider directory,
        string? name = null,
        bool overwrite = false,
        TimeSpan? duration = null,
        object? state = null);

    /// <summary>
    /// Stops an ongoing replay recording.
    /// </summary>
    void StopRecording();

    /// <summary>
    /// Returns information about the currently ongoing replay recording.
    /// </summary>
    ReplayRecordingStats GetReplayStats();

    /// <summary>
    /// Returns a task that will wait for all the current writing tasks to finish.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we are currently recording (<see cref="IsRecording"/> true).
    /// </exception>
    Task WaitWriteTasks();

    /// <summary>
    /// Returns true if there are any currently running write tasks.
    /// </summary>
    bool IsWriting();
}

/// <summary>
/// Event object used by <see cref="IReplayRecordingManager.RecordingStopped2"/>.
/// Allows modifying metadata and adding more data to replay files.
/// </summary>
public sealed class ReplayRecordingStopped
{
    /// <summary>
    /// Mutable metadata that will be saved to the replay's metadata file.
    /// </summary>
    public required MappingDataNode Metadata { get; init; }

    /// <summary>
    /// A writer that allows arbitrary file writing into the replay file.
    /// </summary>
    public required IReplayFileWriter Writer { get; init; }

    internal ReplayRecordingStopped()
    {
    }
}

/// <summary>
/// Event data for <see cref="IReplayRecordingManager.RecordingFinished"/>.
/// </summary>
/// <param name="Directory">The writable dir provider in which the replay is being recorded.</param>
/// <param name="Path">The path to the replay in <paramref name="Directory"/>.</param>
/// <param name="State">The state object passed to <see cref="IReplayRecordingManager.TryStartRecording"/>.</param>
public record ReplayRecordingFinished(IWritableDirProvider Directory, ResPath Path, object? State);

/// <summary>
/// Statistics for an active replay recording.
/// </summary>
/// <param name="Time">The simulation time the replay has been recording for.</param>
/// <param name="Ticks">The amount of simulation ticks the replay has recorded.</param>
/// <param name="Size">The total compressed size of the replay data blobs.</param>
/// <param name="UncompressedSize">The total uncompressed size of the replay data blobs.</param>
public record struct ReplayRecordingStats(TimeSpan Time, uint Ticks, long Size, long UncompressedSize);

/// <summary>
/// Allows writing extra files directly into the replay file.
/// </summary>
/// <seealso cref="ReplayRecordingStopped"/>
/// <seealso cref="IReplayRecordingManager.RecordingStopped2"/>
public interface IReplayFileWriter
{
    /// <summary>
    /// The base directory inside the replay directory you should generally be writing to.
    /// This is equivalent to <see cref="ReplayConstants.ReplayZipFolder"/>.
    /// </summary>
    ResPath BaseReplayPath { get; }

    /// <summary>
    /// Writes arbitrary data into a file in the replay.
    /// </summary>
    /// <param name="path">The file path to write to.</param>
    /// <param name="bytes">The bytes to write to the file.</param>
    /// <param name="compressionLevel">How much to compress the file.</param>
    void WriteBytes(
        ResPath path,
        ReadOnlyMemory<byte> bytes,
        CompressionLevel compressionLevel = CompressionLevel.Optimal);
}

/// <summary>
/// Engine-internal functions for <see cref="IReplayRecordingManager"/>.
/// </summary>
internal interface IReplayRecordingManagerInternal : IReplayRecordingManager
{
    /// <summary>
    /// Initializes the replay manager.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Shut down any active replay recording, at engine shutdown.
    /// </summary>
    void Shutdown();
}
