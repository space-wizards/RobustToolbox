using System.Drawing;
using GorgonLibrary;
using SS13_Shared;

namespace ClientServices.Tiles
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