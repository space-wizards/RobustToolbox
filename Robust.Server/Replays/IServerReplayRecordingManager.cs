using Robust.Shared.Replays;

namespace Robust.Server.Replays;

public interface IServerReplayRecordingManager : IReplayRecordingManager
{
    /// <summary>
    ///     Starts or stops a replay recording. The first tick will contain all game state data that would be sent to a
    ///     new client with PVS disabled. Old messages queued with <see
    ///     cref="IReplayRecordingManager.QueueReplayMessage(object)"/> are NOT included. Those messages are only saved
    ///     once recording has started.
    /// </summary>
    void ToggleRecording();
}
