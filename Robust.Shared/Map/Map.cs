namespace Robust.Shared.Map
{
    internal class Map : IMap
    {
        public MapId Index { get; }

        public Map(MapId mapID)
        {
            Index = mapID;
        }
    }
}
