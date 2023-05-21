using System.Threading.Tasks;
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
    /// <param name="dir">A directory containing the replay files.</param>
    /// <param name="path">The path to the replay's subdirectory.</param>
    public MappingDataNode? LoadYamlMetadata(IWritableDirProvider dir, ResPath path);

    /// <summary>
    /// Async task that loads up a replay for playback.
    /// </summary>
    /// <remarks>
    /// This task is intended to be used with a <see cref="Job{T}"/> so that the loading can happen over several frame
    /// updates. Note that a load is being processed over multiple "ticks", then the normal system tick updating needs
    /// to be blocked by subscribing to  <see cref="IGameController.TickUpdateOverride"/> in order to avoid errors while
    /// systems iterate over pre-init or pre-startup entities.
    /// </remarks>
    /// <param name="dir">A directory containing the replay data that should be loaded.</param>
    /// <param name="path">The path to the replay's subdirectory.</param>
    /// <param name="callback">A callback delegate that invoked to provide information about the current loading
    /// progress. This callback can be used to invoke <see cref="Job{T}.SuspendIfOutOfTime"/>. </param>
    Task<ReplayData> LoadReplayAsync(IWritableDirProvider dir, ResPath path, LoadReplayCallback callback);

    /// <summary>
    /// Async task that loads the initial state of a replay, including spawning & initializing all entities.
    /// </summary>
    /// <remarks>
    /// This task is intended to be used with a <see cref="Job{T}"/> so that the loading can happen over several frame
    /// updates. Note that a load is being processed over multiple "ticks", then the normal system tick updating needs
    /// to be blocked by subscribing to  <see cref="IGameController.TickUpdateOverride"/> in order to avoid errors while
    /// systems iterate over pre-init or pre-startup entities.
    /// </remarks>
    /// <param name="callback">A callback delegate that invoked to provide information about the current loading
    /// progress. This callback can be used to invoke <see cref="Job{T}.SuspendIfOutOfTime"/>. </param>
    Task StartReplayAsync(ReplayData data, LoadReplayCallback callback);

    /// <summary>
    /// Convenience function that combines <see cref="LoadReplayAsync"/> and <see cref="StartReplayAsync"/>
    /// </summary>
    /// <remarks>
    /// This task is intended to be used with a <see cref="Job{T}"/> so that the loading can happen over several frame
    /// updates. Note that a load is being processed over multiple "ticks", then the normal system tick updating needs
    /// to be blocked by subscribing to  <see cref="IGameController.TickUpdateOverride"/> in order to avoid errors while
    /// systems iterate over pre-init or pre-startup entities.
    /// </remarks>
    /// <param name="dir">A directory containing the replay files.</param>
    /// <param name="path">The path to the replay's subdirectory.</param>
    /// <param name="callback">A callback delegate that invoked to provide information about the current loading
    /// progress. This callback can be used to invoke <see cref="Job{T}.SuspendIfOutOfTime"/>. </param>
    Task<ReplayData> LoadAndStartReplayAsync(IWritableDirProvider dir, ResPath path, LoadReplayCallback? callback = null);
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

