using System.Drawing;
using GorgonLibrary;
using SS13_Shared;

namespace ClientServices.Tiles
{
    public class Floor : Tile
    {
        public Floor(TileState state, Vector2D position)
            : base(state, position)
        {
            ConnectSprite = false;
            name = "Floor";

            Sprite = _resourceManager.GetSprite("floor_texture");
            Sprite.SetPosition(position.X, position.Y);
        }
    }
}