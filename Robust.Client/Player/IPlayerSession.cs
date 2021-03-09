using System;
using Robust.Shared.Players;

namespace Robust.Client.Player
{
    /// <summary>
    /// Client side session of a player.
    /// </summary>
    [Obsolete("Use the base " + nameof(ICommonSession))]
    public interface IPlayerSession : ICommonSession { }
}
