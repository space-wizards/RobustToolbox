using Robust.Shared.IoC;
using System.Threading;

namespace Robust.Server.Replays;

public sealed class ReplayRecordingManager : IInternalReplayRecordingManager
{
    /// <inheritdoc/>
    public bool Recording => false;

    /// <inheritdoc/>
    public void Initialize() { }

    /// <inheritdoc/>
    public void ToggleRecording() { }

    /// <inheritdoc/>
    public void QueueReplayMessage(object obj) { }

    /// <inheritdoc/>
    public void SaveReplayData(Thread mainThread, IDependencyCollection parentDeps) { }
}
