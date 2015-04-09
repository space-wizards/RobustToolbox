using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Map
{
    public interface ITileCollection
    {
        Tile this[Vector2 pos] { get; set; }
        Tile this[int x, int y] { get; set; }
    }
}
