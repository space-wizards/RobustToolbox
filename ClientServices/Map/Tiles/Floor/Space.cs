using ClientInterfaces.Lighting;
using ClientInterfaces.Resource;
using ClientServices.Resources;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using System.Drawing;
using ClientInterfaces;
using SS13_Shared;

namespace ClientServices.Map.Tiles.Floor
{
    public class Space : Tile
    {
        public Space(Sprite _sprite, TileState state, float size, Vector2D _position, Point _tilePosition, ILightManager _lightManager,  IResourceManager resourceManager)
            : base(_sprite, state, size, _position, _tilePosition, _lightManager, resourceManager)
        {
            TileType = TileType.Space;
            name = "Space";
        }

        public override void Render(float xTopLeft, float yTopLeft, int tileSpacing, Batch batch)
        {
            return;
        }
    }
}
