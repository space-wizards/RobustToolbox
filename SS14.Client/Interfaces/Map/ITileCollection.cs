using SFML.System;

namespace SS14.Client.Interfaces.Map
{
    public interface ITileCollection
    {
        Tile this[Vector2 pos] { get; set; }
        Tile this[int x, int y] { get; set; }
    }
}
