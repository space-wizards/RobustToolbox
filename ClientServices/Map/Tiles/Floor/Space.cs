using System.Drawing;
using GorgonLibrary;
using SS13_Shared;

namespace ClientServices.Tiles
{
    public class Space : Tile
    {
        public Space(TileState state, Vector2D position)
            : base(state, position)
        {
            ConnectSprite = false;
            name = "Space";

            Sprite = _resourceManager.GetSprite("space_texture");
            Sprite.SetPosition(position.X, position.Y);
        }
    }
}