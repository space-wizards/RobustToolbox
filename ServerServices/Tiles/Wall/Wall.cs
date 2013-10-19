using ServerServices.Map;
using SS13_Shared;

namespace ServerServices.Tiles
{
    public class Wall : Tile
    {
        public Wall(Vector2 pos, MapManager _map)
            : base(pos, _map)
        {
        }

        /*
         public override bool HandleItemClick(Atom.Item.Item item)
         {
             TileState state = tileState;
             if (item.IsTypeOf(typeof(Atom.Item.Tool.Crowbar)))
             {
                 if (tileState == TileState.Wrenched)
                 {
                     tileState = TileState.Dead;
                     tileType = TileType.Floor;
                 }
             }
             else if(item.IsTypeOf(typeof(Atom.Item.Tool.Welder)))
             {
                 if (tileState == TileState.Healthy)
                 {
                     tileState = TileState.Welded;
                 }
                 else if (tileState == TileState.Welded)
                 {
                     tileState = TileState.Healthy;
                 }
             }
             else if (item.IsTypeOf(typeof(Atom.Item.Tool.Wrench)))
             {
                 if (tileState == TileState.Welded)
                 {
                     tileState = TileState.Wrenched;
                 }
                 else if (tileState == TileState.Wrenched)
                 {
                     tileState = TileState.Welded;
                 }
             }
             if (tileState != state)
                 return true;
             return false;
         }
         */ //TODO HOOK THIS BACK UP WITH ENTITY SYSTEM
    }
}