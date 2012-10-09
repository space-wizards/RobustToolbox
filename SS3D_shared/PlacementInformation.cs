using System;

namespace SS13_Shared
{
    public class PlacementInformation
    {
        public int MobUid;                                                //UID of mob this permission is for.
        public ushort Range;                                          //Valid range from mob.

        public int Uses = 1;                                              //How many objects of this type may be placed.

        public Boolean IsTile;

        public string EntityType;                                    //Object name / type. If not tile.
        public string TileType = "";                         //Tile Type if tile.

        public PlacementOption PlacementOption = PlacementOption.AlignNone; //Alignment type. See enum declaration for infos.
    }
}
