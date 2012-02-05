using ClientServices.Resources;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using System.Drawing;
using ClientInterfaces;


namespace ClientServices.Map.Tiles.Floor
{
    public class Floor : Tile
    {
        public Floor(Sprite _sprite, TileState state, float size, Vector2D _position, Point _tilePosition, ILightManager _lightManager, ResourceManager resourceManager)
            : base(_sprite, state, size, _position, _tilePosition, _lightManager, resourceManager)
        {
            tileType = TileType.Floor;
            name = "Floor";
        }


    }
}
