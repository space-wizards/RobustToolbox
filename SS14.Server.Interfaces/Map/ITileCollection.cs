using SFML.System;
using SS14.Shared;
using SS14.Shared.Maths;

namespace SS14.Server.Interfaces.Map
{
    public interface ITileCollection
    {
        Tile this[Vector2f pos] { get; set; }
        Tile this[int x, int y] { get; set; }
    }
}
