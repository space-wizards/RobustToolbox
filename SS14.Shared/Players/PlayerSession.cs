using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Players;

namespace SS14.Shared.Players
{
    internal class PlayerSession : IPlayerSession
    {
        public INetChannel NetChannel { get; }

        public int Entity { get; }
    }
}
