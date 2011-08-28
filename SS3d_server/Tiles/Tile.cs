using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3d_server.Tiles.Atmos;
using SS3d_server.Modules;
using SS3d_server.Modules.Map;

namespace SS3d_server.Tiles
{
    public class Tile
    {
        public TileType tileType;
        public GasCell gasCell;
        public TileState tileState;
        public bool gasPermeable = false;
        public bool gasSink = false;
        private Map map;
        private int _x;
        private int _y;

        public Tile(int x, int y, Map _map)
        {
            tileState = TileState.Healthy;
            map = _map;
            _x = x;
            _y = y;
        }

        // These return true if the tile needs to be updated over the network
        public bool ClickedBy(Atom.Atom clicker)
        {
            if (clicker == null)
                return false;
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

        public void AddDecal(DecalType type)
        {
            var message = map.CreateMapMessage(MapMessage.TurfAddDecal);
            message.Write(_x);
            message.Write(_y);
            message.Write((byte)type);
            map.SendMessage(message);
        }

        public virtual bool HandleItemClick(Atom.Item.Item item)
        {
            return false;
        }

        
    }
}
