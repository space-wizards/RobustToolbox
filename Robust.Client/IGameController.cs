using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Robust.Client;

public interface IGameController
{
    InitialLaunchState LaunchState { get; }

    void Shutdown(string? reason=null);

    /// <summary>
    ///     Try to cause the launcher to either reconnect to the same server or connect to a new server.
    ///     *The engine will shutdown on success.*
    ///     Will throw an exception if contacting the launcher failed (success indicates it is now the launcher's responsibility).
    ///     To redial the same server, retrieve the server's address from `LaunchState.Ss14Address`.
    /// </summary>
    /// <param name="address">The server address, such as "ss14://localhost:1212/".</param>
    /// <param name="text">Informational text on the cause of the reconnect. Empty or null gives a default reason.</param>
    void Redial(string address, string? text = null);

    /// <summary>
    ///     This event gets invoked prior to performing entity tick update logic. If this is null the game
    ///     controller will simply call <see cref="IEntityManager.TickUpdate(float, bool, Prometheus.Histogram?)"/>.
    ///     This exists to give content module more control over tick updating.
    /// </summary>
    event Action<FrameEventArgs>? ContentEntityTickUpdate;
}

