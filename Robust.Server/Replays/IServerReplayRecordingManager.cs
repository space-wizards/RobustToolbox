using Robust.Shared.Replays;
using System;
using Robust.Shared;
using Robust.Shared.ContentPack;

namespace Robust.Server.Replays;

public interface IServerReplayRecordingManager : IReplayRecordingManager
{
    /// <summary>
    ///     Starts recording a replay.
    /// </summary>
    /// <param name="path">
    /// The folder where the replay will be stored. This will be some folder within  <see cref="CVars.ReplayDirectory"/>.
    /// If not provided, will default to using the current time.
    /// </param>
    /// <param name="overwrite">
    /// Whether to overwrite the specified path if a folder already exists.
    /// </param>
    /// <param name="duration">
    /// Optional time limit for the recording.
    /// </param>
    /// <returns>Returns true if the recording was successfully started.</returns>
    bool TryStartRecording(IWritableDirProvider directory, string? path = null, bool overwrite = false, TimeSpan? duration = null);

    void StopRecording();

    /// <summary>
    ///     Returns information about the currently ongoing replay recording, including the currently elapsed time and the compressed replay size.
    /// </summary>
    (float Minutes, int Ticks, float Size, float UncompressedSize) GetReplayStats();
}
