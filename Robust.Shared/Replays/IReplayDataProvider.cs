using System;
using Robust.Shared.GameStates;

namespace Robust.Shared.Replays;

/// <summary>
/// Provides access to the per-tick game states and messages of a replay.
/// </summary>
/// <remarks>
/// This abstraction exists so that the playback code does not need to keep the entire replay
/// (all <see cref="GameState"/>s and <see cref="ReplayMessage"/>s) resident in memory at once. A
/// large replay can deserialize to tens of gigabytes; the client provides a windowed implementation
/// (<c>BufferedReplayDataProvider</c>) that only keeps a handful of data blocks loaded at a time.
/// </remarks>
public interface IReplayDataProvider : IDisposable
{
    /// <summary>
    /// The total number of ticks (states/messages) in the replay.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Get the game state for a given tick index. May trigger a (synchronous) load from disk.
    /// </summary>
    GameState GetState(int index);

    /// <summary>
    /// Get the networked messages for a given tick index. May trigger a (synchronous) load from disk.
    /// </summary>
    ReplayMessage GetMessages(int index);
}
