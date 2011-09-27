using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using System.Drawing;
using ClientInterfaces;


namespace ClientMap.Tiles.Floor
{
    public class Floor : Tile
    {
        public Floor(Sprite _sprite, TileState state, float size, Vector2D _position, Point _tilePosition, ILightManager _lightManager)
            : base(_sprite, state, size, _position, _tilePosition, _lightManager)
        {
            tileType = TileType.Floor;
            name = "Floor";
        }


    }
}
