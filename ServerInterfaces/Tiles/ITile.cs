using SS13_Shared;
using ServerInterfaces.Atmos;

using System;


namespace ServerInterfaces.Tiles
{
    public delegate void TileChangeHandler(Type tNew);
    public interface ITile
    {
        event TileChangeHandler TileChange; //This event will be used for wall mounted objects and
        void RaiseChangedEvent(Type type);
        void AddDecal(DecalType type);
        TileState TileState { get; set; }
        IGasCell GasCell { get; set; }
        bool StartWithAtmos { get; }
        bool GasPermeable { get; }
        bool GasSink { get; }
    }
}