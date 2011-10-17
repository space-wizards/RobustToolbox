using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerServices.Tiles.Atmos;
using ServerServices.Map;

namespace ServerServices.Tiles
{
    public class Tile
    {
        public TileType tileType;
        public GasCell gasCell;
        public TileState tileState;
        public bool gasPermeable = false;
        public bool gasSink = false;
        private Map.Map map;
        private int _x;
        private int _y;

        public delegate void TileChangeHandler(TileType tNew);

        public event TileChangeHandler TileChange; //This event will be used for wall mounted objects and
                                                   //other things that need to react to tiles changing.
        public void RaiseChangedEvent(TileType type)
        {
            if(TileChange != null) TileChange(type);
        }

        public Tile(int x, int y, Map.Map _map)
        {
            tileState = TileState.Healthy;
            map = _map;
            _x = x;
            _y = y;
        }

        // These return true if the tile needs to be updated over the network
        /*
        public bool ClickedBy(Atom.Atom clicker)
        {
            if (clicker == null)
                return false;
             if(clicker.IsChildOfType(typeof(Atom.Mob.Mob)))
             {
                 Atom.Mob.Mob mob = (Atom.Mob.Mob)clicker;
                 return ClickedBy(mob.selectedAppendage.heldItem);
             }
             else if (clicker.IsChildOfType(typeof(Atom.Item.Item)))
             {
                 return HandleItemClick((Atom.Item.Item)clicker);
             }
             return false;
        }
        */ //TODO HOOK ME BACK UP WITH ENTITY SYSTEM

        public void AddDecal(DecalType type)
        {
            var message = map.CreateMapMessage(MapMessage.TurfAddDecal);
            message.Write(_x);
            message.Write(_y);
            message.Write((byte)type);
            map.SendMessage(message);
        }

        /*
        public virtual bool HandleItemClick(Atom.Item.Item item)
        {
            return false;
        }*/ //TODO HOOK ME BACK UP WITH ENTITY SYSTEM

        
    }
}
