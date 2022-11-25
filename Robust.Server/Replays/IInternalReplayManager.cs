using Robust.Shared.IoC;
using System.Threading;

namespace Robust.Server.Replays;

internal interface IInternalReplayRecordingManager : IServerReplayRecordingManager
{
    /// <summary>
    ///     Initializes the replay manager.
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Saves the replay data for the current tick. Does nothing if <see
    ///     cref="IServerReplayRecordingManager.Recording"/> is false.
    /// </summary>
    /// <remarks>
    ///     This is intended to be called by PVS in parallel with other game-state networking.
    /// </remarks>
    void SaveReplayData(Thread mainThread, IDependencyCollection parentDeps);
}
