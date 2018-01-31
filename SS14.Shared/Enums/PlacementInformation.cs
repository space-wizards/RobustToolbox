using SS14.Shared.GameObjects;

namespace SS14.Shared.Enums
{
    public class PlacementInformation
    {
        public string EntityType { get; set; }
        public bool IsTile { get; set; }
        public EntityUid MobUid { get; set; }
        public string PlacementOption { get; set; }
        public int Range { get; set; }
        public ushort TileType { get; set; }
        public int Uses { get; set; } = 1;

        private ushort tileType; //Tile Type if tile.
        private int uses = 1; //How many objects of this type may be placed.

       
    }
}
