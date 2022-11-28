using Robust.Shared.GameObjects;

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
}
