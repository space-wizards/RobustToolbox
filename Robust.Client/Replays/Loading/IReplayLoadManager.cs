using System;
using System.Threading.Tasks;
using Robust.Client.Replays.Commands;
using Robust.Client.Replays.Playback;
using Robust.Shared.ContentPack;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

namespace Robust.Client.Replays.Loading;

public interface IReplayLoadManager
{
    public void Initialize();

    /// <summary>
    /// Load metadata information from a replay's yaml file.
    /// </summary>
    /// <param name="fileReader">A reader containing the replay files. Disposed when loading is done.</param>
    public MappingDataNode? LoadYamlMetadata(IReplayFileReader fileReader);

    /// <summary>
    /// Async task that loads up a replay for playback. Note that this will have some side effects, such as loading
    /// networked resources and prototypes. These resources are not tracked or automatically unloaded.
    /// </summary>
    /// <remarks>
    /// This task is intended to be used with a <see cref="Job{T}"/> so that the loading can happen over several frame
    /// updates.
    /// </remarks>
    /// <param name="fileReader">A reader containing the replay files. Disposed when loading is done.</param>
    /// <param name="callback">A callback delegate that invoked to provide information about the current loading
    /// progress. This callback can be used to invoke <see cref="Job{T}.SuspendIfOutOfTime"/>. </param>
    Task<ReplayData> LoadReplayAsync(IReplayFileReader fileReader, LoadReplayCallback callback);

    /// <summary>
    /// Async task that loads the initial state of a replay, including spawning and initializing all entities. Note that
    /// this will have some side effects, such as loading networked resources and prototypes. These resources are not
    /// tracked or automatically unloaded.
    /// </summary>
    /// <remarks>
    /// This task is intended to be used with a <see cref="Job{T}"/> so that the loading can happen over several frame
    /// updates, otherwise, you could simply start a replay via <see cref="IReplayPlaybackManager.StartReplay"/>..
    /// </remarks>
    /// <param name="callback">A callback delegate that invoked to provide information about the current loading
    /// progress. This callback can be used to invoke <see cref="Job{T}.SuspendIfOutOfTime"/>. </param>
    Task StartReplayAsync(ReplayData data, LoadReplayCallback callback);

    /// <summary>
    /// Convenience function that combines <see cref="LoadReplayAsync"/> and <see cref="StartReplayAsync"/>.
    /// </summary>
    /// <remarks>
    /// This task is intended to be used with a <see cref="Job{T}"/> so that the loading can happen over several frame
    /// updates.
    /// </remarks>
    /// <param name="fileReader">A reader containing the replay files. Disposed when loading is done.</param>
    /// <param name="callback">A callback delegate that invoked to provide information about the current loading
    /// progress. This callback can be used to invoke <see cref="Job{T}.SuspendIfOutOfTime"/>. </param>
    Task LoadAndStartReplayAsync(IReplayFileReader fileReader, LoadReplayCallback? callback = null);

    /// <summary>
    /// This is a variant of <see cref="LoadAndStartReplayAsync"/> that will first invoke <see cref="LoadOverride"/>
    /// before defaulting to simply simply running <see cref="LoadAndStartReplayAsync"/> synchronously.
    /// </summary>
    void LoadAndStartReplay(IReplayFileReader fileReader);

    /// <summary>
    /// Event that can be used to override the default replay loading behaviour.
    /// </summary>
    /// <remarks>
    /// E.g., this could be used to make the <see cref="ReplayLoadCommand"/> switch to some loading screen with an async
    /// load job, rather than just hanging the client.
    /// </remarks>
    event Action<IReplayFileReader>? LoadOverride;
}

public delegate Task LoadReplayCallback(float current, float max, LoadingState state, bool forceSuspend);

/// <summary>
/// Enum used to indicate loading progress.
/// </summary>
public enum LoadingState : byte
{
    ReadingFiles,
    ProcessingFiles,
    Spawning,
    Initializing,
    Starting,
}

