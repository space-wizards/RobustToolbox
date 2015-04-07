using GorgonLibrary;
using System.Drawing;

namespace SS14.Client.Interfaces.Map
{
    public interface ITileCollection
    {
        Tile this[Vector2D pos] { get; set; }
        Tile this[int x, int y] { get; set; }
    }
}
