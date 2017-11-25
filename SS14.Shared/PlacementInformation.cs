using System;

namespace SS14.Shared
{
    public class PlacementInformation
    {
        private string entityType; //Object name / type. If not tile.
        private Boolean isTile;
        private int mobUid; //UID of mob this permission is for.
        private string placementOption;
        private int range; //Valid range from mob.

        private ushort tileType; //Tile Type if tile.
        private int uses = 1; //How many objects of this type may be placed.

        public string EntityType { get => entityType; set => entityType = value; }
        public bool IsTile { get => isTile; set => isTile = value; }
        public int MobUid { get => mobUid; set => mobUid = value; }
        public string PlacementOption { get => placementOption; set => placementOption = value; }
        public int Range { get => range; set => range = value; }
        public ushort TileType { get => tileType; set => tileType = value; }
        public int Uses { get => uses; set => uses = value; }
    }
}
