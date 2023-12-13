using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Robust.Server.Configuration;

/// <summary>
/// A networked configuration manager that controls the replication of
/// console variables between client and server.
/// </summary>
public interface IServerNetConfigurationManager : INetConfigurationManager
{
    /// <summary>
    /// Synchronize the CVars marked with <see cref="CVar.REPLICATED"/> with the client.
    /// This needs to be called once during the client connection.
    /// </summary>
    /// <param name="client">Client's NetChannel to sync replicated CVars with.</param>
    void SyncConnectingClient(INetChannel client);
}
