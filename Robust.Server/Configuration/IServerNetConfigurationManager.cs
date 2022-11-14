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
    /// Get a replicated client CVar for a specific client.
    /// </summary>
    /// <typeparam name="T">CVar type.</typeparam>
    /// <param name="channel">channel of the connected client.</param>
    /// <param name="definition">The CVar.</param>
    /// <returns>Replicated CVar of the client.</returns>
    public T GetClientCVar<T>(INetChannel channel, CVarDef<T> definition) where T : notnull =>
        GetClientCVar<T>(channel, definition.Name);

    /// <summary>
    /// Get a replicated client CVar for a specific client.
    /// </summary>
    /// <typeparam name="T">CVar type.</typeparam>
    /// <param name="channel">channel of the connected client.</param>
    /// <param name="name">Name of the CVar.</param>
    /// <returns>Replicated CVar of the client.</returns>
    T GetClientCVar<T>(INetChannel channel, string name);

    /// <summary>
    /// Synchronize the CVars marked with <see cref="CVar.REPLICATED"/> with the client.
    /// This needs to be called once during the client connection.
    /// </summary>
    /// <param name="client">Client's NetChannel to sync replicated CVars with.</param>
    void SyncConnectingClient(INetChannel client);
}
