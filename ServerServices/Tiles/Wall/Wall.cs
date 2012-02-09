using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;

namespace ServerServices.Tiles.Wall
{
    public class Wall : Tile
    {
        public Wall(int x, int y, ServerServices.Map.Map _map)
            : base(x, y, _map)
        {
            tileType = TileType.Wall;
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
