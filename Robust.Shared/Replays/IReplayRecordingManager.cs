using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Markdown.Mapping;
using System;
using System.Collections.Generic;

namespace Robust.Shared.Replays;

public interface IReplayRecordingManager
{
    /// <summary>
    ///     Queues some net-serializable data to be saved for replaying
    /// </summary>
    /// <remarks>
    ///     The queued object is typically something like an <see cref="EntityEventArgs"/>, so that replays can
    ///     simulate receiving networked messages. However, this can really be any serializable data and could be used
    ///     for saving server-exclusive data like power net or atmos pipe-net data for replaying. Alternatively, those
    ///     could also just use networked component states on entities that are in null space and hidden from all
    ///     players (but still saved to replays).
    /// </remarks>
    void QueueReplayMessage(object args);

    /// <summary>
    ///     Whether the server is currently recording replay data.
    /// </summary>
    bool Recording { get; }

    /// <summary>
    ///     This gets invoked whenever a replay recording starts. Subscribers can use this to add extra yaml metadata
    ///     data to the recording, as well as to effectively "raise" networked events that would get sent to a newly
    ///     connecting "client".
    /// </summary>
    event Action<(MappingDataNode, List<object>)>? OnRecordingStarted;

    /// <summary>
    ///     This gets invoked whenever a replay recording ends. Subscribers can use this to add extra yaml metadata data to the recording.
    /// </summary>
    event Action<MappingDataNode>? OnRecordingStopped;
}
