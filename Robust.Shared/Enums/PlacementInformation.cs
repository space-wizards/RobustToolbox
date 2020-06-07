using Robust.Shared.GameObjects;

namespace Robust.Shared.Enums
{
    public class PlacementInformation
    {
        public string? EntityType { get; set; }
        public bool IsTile { get; set; }
        public EntityUid MobUid { get; set; }
        public string? PlacementOption { get; set; }
        public int Range { get; set; }
        public ushort TileType { get; set; }
        public int Uses { get; set; } = 1;
    }
}
