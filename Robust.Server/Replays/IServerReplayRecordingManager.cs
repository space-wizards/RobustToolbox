using Robust.Shared.Replays;

namespace Robust.Server.Replays;

public interface IServerReplayRecordingManager : IReplayRecordingManager
{
    /// <summary>
    /// Processes pending write tasks and saves the replay data for the current tick. This should be called even if a
    /// replay is not currently being recorded.
    /// </summary>
    void Update();
}
