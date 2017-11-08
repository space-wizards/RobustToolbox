using SS14.Shared;
using SS14.Shared.Interfaces.Network;

namespace SS14.Client.Player
{
    public class PlayerSession
    {
        private PlayerManager _manager;

        public SessionStatus Status { get; set; } = SessionStatus.Zombie;
        public int NetID { get; }
        public long Uuid { get; }
        public string Name { get; set; } = "<Unknown>";
        public short Ping { get; set; }

        public PlayerSession(PlayerManager manager, int netId, long uuid)
        {
            _manager = manager;
            NetID = netId;
            Uuid = uuid;
        }

    }
}
