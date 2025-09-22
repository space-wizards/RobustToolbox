using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Player;

/// <summary>
///     Stores session-specific data that is not lost upon reconnect.
/// </summary>
public sealed class SessionData
{
    public SessionData(NetUserId userId, string userName)
    {
        UserId = userId;
        UserName = userName;
    }

    /// <summary>
    ///     The session ID of the player owning this data.
    /// </summary>
    [ViewVariables]
    public NetUserId UserId { get; }

    [ViewVariables]
    public string UserName { get; }

    /// <summary>
    ///     Custom field that content can assign anything to.
    ///     Go wild.
    /// </summary>
    [ViewVariables]
    public object? ContentDataUncast { get; set; }
}