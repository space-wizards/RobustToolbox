using ClientInterfaces.Lighting;
using ClientInterfaces.Resource;
using ClientServices.Resources;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using System.Drawing;
using ClientInterfaces;
using SS13_Shared;
using ClientInterfaces.Resource;
using SS13.IoC;

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
