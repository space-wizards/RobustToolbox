using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.GameObjects;

namespace SS14.Server.Interfaces.Player
{
    /// <summary>
    ///     Stores player-specific data that is not lost upon reconnect.
    /// </summary>
    public interface IPlayerData
    {
        EntityUid? AttachedEntityUid { get; }

        /// <summary>
        ///     Custom field that content can assign anything to.
        ///     Go wild.
        /// </summary>
        object ContentData { get; set; }
    }
}
