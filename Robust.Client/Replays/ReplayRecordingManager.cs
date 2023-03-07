using Robust.Shared.Replays;
using Robust.Shared.Serialization.Markdown.Mapping;
using System.Collections.Generic;
using System;

namespace Robust.Client.Replays;

/// <summary>
///     Dummy class so that <see cref="IReplayRecordingManager"/> can be used in shared code.
/// </summary>
public sealed class ReplayRecordingManager : IReplayRecordingManager
{
    /// <inheritdoc/>
    public void QueueReplayMessage(object args) { }

    public bool Recording => false;

    /// <inheritdoc/>
    public event Action<(MappingDataNode, List<object>)>? OnRecordingStarted
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public event Action<MappingDataNode>? OnRecordingStopped
    {
        add { }
        remove { }
    }

}
