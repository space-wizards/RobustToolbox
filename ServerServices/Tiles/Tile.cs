using ServerInterfaces.Tiles;
using ServerInterfaces.Atmos;
using ServerServices.Atmos;
using SS13_Shared;
using System;

namespace ServerServices.Tiles
{
    public abstract class Tile : ITile
    {
        public GasCell gasCell;
        public TileState TileState { get; set; }
        private bool gasPermeable = false;
        private bool gasSink = false;
        private bool startWithAtmos = false; //Does this start with  breathable atmosphere? Maybe turn this into a bitfield to define which gases it starts with.
        private Map.MapManager map;
        private int _x;
        private int _y;
        
        public event TileChangeHandler TileChange; //This event will be used for wall mounted objects and
                                                   //other things that need to react to tiles changing.
        public void RaiseChangedEvent(Type type)
        {
            if(TileChange != null) TileChange(type);
        }

        public Tile(int x, int y, Map.MapManager _map)
        {
            TileState = TileState.Healthy;
            map = _map;
            _x = x;
            _y = y;
        }


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
        }*/
        //TODO HOOK ME BACK UP WITH ENTITY SYSTEM

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
        */
        //TODO HOOK ME BACK UP WITH ENTITY SYSTEM


        #region getters / setters
        public IGasCell GasCell
        {
            get
            {
                return (IGasCell)gasCell;
            }
            set
            {
                gasCell = (GasCell)value;
            }
        }

        public bool StartWithAtmos
        {
            get
            {
                return startWithAtmos;
            }
            set
            {
                startWithAtmos = value;
            }
        }

        public bool GasPermeable
        {
            get
            {
                return gasPermeable;
            }
            set
            {
                gasPermeable = value;
            }
        }

        public bool GasSink
        {
            get
            {
                return gasSink;
            }
            set
            {
                gasSink = value;
            }
        }
        #endregion

    }
}
