using SS14.Shared;
using System.Drawing;

namespace SS14.Client.Services.Tiles
{
    public class Space : Tile
    {
        public Space(TileState state, RectangleF rect)
            : base(state, rect)
        {
            ConnectSprite = false;
            name = "Space";

            Sprite = _resourceManager.GetSprite("space_texture");
            Sprite.SetPosition(Position.X, Position.Y);
        }
    }
}