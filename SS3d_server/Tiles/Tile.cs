using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3d_server.Tiles.Atmos;

namespace SS3d_server.Tiles
{
    public class Tile
    {
        public TileType tileType;
        public GasCell gasCell;
        public TileState tileState;
        public bool gasPermeable = false;
        public bool gasSink = false;

        public Tile()
        {
            tileState = TileState.Healthy;
        }

        // These return true if the tile needs to be updated over the network
        public bool ClickedBy(Atom.Atom clicker)
        {
             if(clicker.IsChildOfType(typeof(Atom.Mob.Mob)))
             {
                 Atom.Mob.Mob mob = (Atom.Mob.Mob)clicker;
                 Console.WriteLine("recall clickby ");
                 return ClickedBy(mob.selectedAppendage.heldItem);
             }
             else if (clicker.IsChildOfType(typeof(Atom.Item.Item)))
             {
                 Console.WriteLine("handle item click " + tileType);
                 return HandleItemClick((Atom.Item.Item)clicker);
             }
             return false;
        }

        public virtual bool HandleItemClick(Atom.Item.Item item)
        {
            return false;
        }

        
    }
}
