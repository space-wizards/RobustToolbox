using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.GameObjects;
using SS14.Shared.Network;

namespace SS14.Server.Interfaces.Player
{
    /// <summary>
    ///     Stores player-specific data that is not lost upon reconnect.
    /// </summary>
    public interface IPlayerData
    {
        /// <summary>
        ///     The session ID of the player owning this data.
        /// </summary>
        NetSessionId SessionId { get; }
        
        /// <summary>
        ///     Custom field that content can assign anything to.
        ///     Go wild.
        /// </summary>
        object ContentDataUncast { get; set; }
    }
}
