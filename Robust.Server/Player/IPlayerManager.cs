using Robust.Shared.Input;
using Robust.Shared.Player;

namespace Robust.Server.Player;

/// <summary>
///     Manages each players session when connected to the server.
/// </summary>
public interface IPlayerManager : ISharedPlayerManager
{
    BoundKeyMap KeyMap { get; }
}