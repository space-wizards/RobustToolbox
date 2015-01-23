using SS14.Shared;
using System.Drawing;

namespace SS14.Client.Services.Tiles
{
    public class Floor : Tile
    {
        public Floor(TileState state, RectangleF rect)
            : base(state, rect)
        {
            ConnectSprite = false;
            name = "Floor";

            Sprite = _resourceManager.GetSprite("floor_texture");
            Sprite.SetPosition(Position.X, Position.Y);
        }
    }
}