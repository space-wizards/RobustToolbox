using Robust.Shared.Replays;
using System;
using Robust.Shared;

namespace Robust.Server.Replays;

public interface IServerReplayRecordingManager : IReplayRecordingManager
{
    void ToggleRecording();

    /// <summary>
    ///     Starts recording a replay.
    /// </summary>
    /// <param name="replayName">
    /// The name of the replay. This determines the name of the <see cref="CVars.ReplayDirectory"/> subfolder where the
    /// data will be saved. If not provided, will default to using the current time.
    /// </param>
    /// <param name="overwrite">
    /// Whether to overwrite the folder, if it already exists.
    /// </param>
    /// <param name="duration">
    /// Optional time limit for the recording.
    /// </param>
    /// <returns>Returns true if the recording was successfully started.</returns>
    bool TryStartRecording(string? replayName = null, bool overwrite = false, TimeSpan? duration = null);

    void StopRecording();

    /// <summary>
    ///     Returns information about the currently ongoing replay recording, including the currently elapsed time and the compressed replay size.
    /// </summary>
    (float Minutes, int Ticks, float Size, float UncompressedSize) GetReplayStats();
}
