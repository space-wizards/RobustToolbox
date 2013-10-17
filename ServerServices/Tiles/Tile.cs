using System;
using Lidgren.Network;
using SS13_Shared;
using ServerInterfaces.Atmos;
using ServerInterfaces.Tiles;
using ServerServices.Atmos;
using ServerServices.Map;

namespace ServerServices.Tiles
{
    public abstract class Tile : ITile
    {
        private readonly int _x;
        private readonly int _y;
        private readonly Vector2 _worldPosition;
        private readonly MapManager map;
        public GasCell gasCell;

        public Tile(int x, int y, MapManager _map)
        {
            TileState = TileState.Healthy;
            map = _map;
            _x = x;
            _y = y;
            _worldPosition =  new Vector2(x * _map.tileSpacing, y * _map.tileSpacing);
        }

        #region ITile Members

        public TileState TileState { get; set; }

        public event TileChangeHandler TileChange; //This event will be used for wall mounted objects and
        //other things that need to react to tiles changing.
        public void RaiseChangedEvent(Type type)
        {
            if (TileChange != null) TileChange(type);
        }


        public void AddDecal(DecalType type)
        {
            NetOutgoingMessage message = map.CreateMapMessage(MapMessage.TurfAddDecal);
            message.Write(_x);
            message.Write(_y);
            message.Write((byte) type);
            map.SendMessage(message);
        }

        #endregion

        #region getters / setters

        public IGasCell GasCell
        {
            get { return gasCell; }
            set { gasCell = (GasCell) value; }
        }

        public bool StartWithAtmos { get; set; }

        public bool GasPermeable { get; set; }

        public bool GasSink { get; set; }

        public Vector2 worldPosition
        {
            get { return _worldPosition; }
        }

        #endregion

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
    }
}