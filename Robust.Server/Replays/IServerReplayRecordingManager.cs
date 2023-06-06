using Robust.Shared.Replays;

namespace Robust.Server.Replays;

public interface IServerReplayRecordingManager : IReplayRecordingManager
{
    /// <summary>
    /// Saves the replay data for the current tick. Does nothing if <see cref="IReplayRecordingManager.IsRecording"/> is false.
    /// </summary>
    void SaveReplayData();
}
