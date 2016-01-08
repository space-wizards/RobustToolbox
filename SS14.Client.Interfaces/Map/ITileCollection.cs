using SFML.System;
using SS14.Shared.Maths;
using System.Drawing;

namespace SS14.Client.Interfaces.Map
{
    public interface ITileCollection
    {
        Tile this[Vector2f pos] { get; set; }
        Tile this[int x, int y] { get; set; }
    }
}
