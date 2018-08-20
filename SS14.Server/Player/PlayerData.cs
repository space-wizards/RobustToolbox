using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;

namespace SS14.Server.Player
{
    class PlayerData : IPlayerData
    {
        public EntityUid? AttachedEntityUid { get; set; }

        public object ContentData { get; set; }
    }
}
