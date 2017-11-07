using SS14.Shared;
using SS14.Shared.Interfaces.Network;

namespace SS14.Client.Player
{
    public class PlayerSession
    {
        private PlayerManager _manager;
        private INetChannel _netChannel;

        public SessionStatus Status { get; }
        public int NetID { get; }
        public string Name { get; }

        public PlayerSession(PlayerManager manager, INetChannel channel) { }
    }
}
