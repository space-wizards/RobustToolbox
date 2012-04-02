using SS13_Shared;

namespace ServerInterfaces.Tiles
{
    public delegate void TileChangeHandler(TileType tNew);
    public interface ITile
    {
        event TileChangeHandler TileChange; //This event will be used for wall mounted objects and
        void RaiseChangedEvent(TileType type);
        void AddDecal(DecalType type);


        TileType tileType { get; set; }

        TileState tileState { get; set; }
    }
}