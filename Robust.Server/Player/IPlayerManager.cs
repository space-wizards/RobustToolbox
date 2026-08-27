using Robust.Shared.Input;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Robust.Server.Player;

/// <summary>
///     Manages each players session when connected to the server.
/// </summary>
[NotContentImplementable]
public interface IPlayerManager : ISharedPlayerManager
{
    BoundKeyMap KeyMap { get; }

    internal void MarkPlayerResourcesSent(INetChannel channel);
}
