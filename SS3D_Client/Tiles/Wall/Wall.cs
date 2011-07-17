using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace SS3D.Tiles.Wall
{
    public class Wall : Tile
    {

        public Wall(Sprite _sprite, Sprite _side, float size, Vector2D _position, Point _tilePosition)
            : base(_sprite, _side, size, _position, _tilePosition)
        {
            tileType = TileType.Wall;
            name = "Wall";
            sprite = _sprite;
            sideSprite = _side;
        }

        public override void Render(float xTopLeft, float yTopLeft, int tileSpacing, bool lighting)
        {
            if (Visible && ((surroundDirs&4) == 0))
            {
                sideSprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                if (!lighting)
                {
                    sideSprite.Color = Color.White;
                }
                else
                {
                    sideSprite.Color = color;
                    ShadeCorners(sideSprite);
                }
                sideSprite.Draw();
            }
        }

        public override void RenderTop(float xTopLeft, float yTopLeft, int tileSpacing, bool lighting)
        {
            if (Visible)
            {
                sprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                sprite.Position -= new Vector2D(0, tileSpacing);
                if (!lighting)
                {
                    sprite.Color = Color.White;
                }
                else
                {
                    sprite.Color = color;
                    ShadeCorners(sprite);
                }
                sprite.Draw();
            }
        }
    }
}
