using SS14.Shared;
using SS14.Shared.Players;

namespace SS14.Client.Player
{
    public class PlayerSession
    {
        private PlayerManager _manager;

        public SessionStatus Status { get; set; } = SessionStatus.Zombie;
        public PlayerIndex Index { get; }
        public long Uuid { get; }
        public string Name { get; set; } = "<Unknown>";
        public short Ping { get; set; }

        public PlayerSession(PlayerManager manager, PlayerIndex index, long uuid)
        {
            _manager = manager;
            Index = index;
            Uuid = uuid;
        }

    }
}
