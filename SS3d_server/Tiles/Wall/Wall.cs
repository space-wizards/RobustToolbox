using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_Server.Tiles.Wall
{
    public class Wall : Tile
    {
        public Wall(int x, int y, SS3D_Server.Modules.Map.Map _map)
            : base(x, y, _map)
        {
            tileType = TileType.Wall;
        }

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
    }
}
