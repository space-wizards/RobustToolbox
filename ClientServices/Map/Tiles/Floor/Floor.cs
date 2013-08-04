using System.Drawing;
using GorgonLibrary;
using SS13_Shared;

namespace ClientServices.Tiles
{
    public class Floor : Tile
    {
        public Floor(TileState state, Vector2D position, Point tilePosition)
            : base(state, position, tilePosition)
        {
            ConnectSprite = false;
            name = "Floor";

            Sprite = _resourceManager.GetSprite("floor_texture");
            Sprite.SetPosition(position.X, position.Y);
        }
    }
}