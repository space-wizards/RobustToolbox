using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;

namespace Robust.Client.Configuration;

/// <summary>
/// A networked configuration manager that controls the replication of
/// console variables between client and server.
/// </summary>
public interface IClientNetConfigurationManager : INetConfigurationManager
{
    /// <summary>
    /// Synchronize the CVars marked with <see cref="CVar.REPLICATED"/> with the server.
    /// This needs to be called once when connecting.
    /// </summary>
    void SyncWithServer();

    /// <summary>
    ///     Clears internal flag for <see cref="ReceivedInitialNwVars"/>.
    ///     Must be called upon disconnect.
    /// </summary>
    void ClearReceivedInitialNwVars();

    public event EventHandler ReceivedInitialNwVars;
}
