using Robust.Shared.Replays;

namespace Robust.Client.Replays;

/// <summary>
///     Dummy class so that <see cref="IReplayRecordingManager"/> can be used in shared code.
/// </summary>
public sealed class ReplayRecordingManager : IReplayRecordingManager
{
    /// <inheritdoc/>
    public void QueueReplayMessage(object args) { }

    public bool Recording => false;
}
