using ClientServices.Resources;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using System.Drawing;
using ClientInterfaces;

namespace ClientServices.Map.Tiles.Floor
{
    public class Space : Tile
    {
        public Space(Sprite _sprite, TileState state, float size, Vector2D _position, Point _tilePosition, ILightManager _lightManager,  ResourceManager resourceManager)
            : base(_sprite, state, size, _position, _tilePosition, _lightManager, resourceManager)
        {
            tileType = TileType.Space;
            name = "Space";
        }

        public override void Render(float xTopLeft, float yTopLeft, int tileSpacing)
        {
            return;
        }
    }
}
