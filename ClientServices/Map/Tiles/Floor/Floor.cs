using System.Drawing;
using GorgonLibrary;
using SS13_Shared;

namespace ClientServices.Tiles
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