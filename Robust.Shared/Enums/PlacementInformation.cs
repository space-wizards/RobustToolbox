using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Shared.Enums
{
    public sealed class PlacementInformation
    {
        public string? EntityType { get; set; }
        public bool IsTile { get; set; }
        public EntityUid MobUid { get; set; }
        public string? PlacementOption { get; set; }
        public int Range { get; set; }
        public ushort TileType { get; set; }
        public TileFlag TileFlags { get; set; }
        public int Uses { get; set; } = 1;
    }
}
