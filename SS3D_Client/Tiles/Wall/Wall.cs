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
                if (lights.Count > 0)
                {
                    if (lighting)
                    {
                        System.Drawing.Color col = System.Drawing.Color.White;
                        foreach (Atom.Light l in lights)
                        {
                            col = Blend(col, l.color, 0.5d);
                        }
                        sideSprite.Color = col;
                    }
                    else
                    {
                        sideSprite.Color = System.Drawing.Color.White;
                    }
                    sideSprite.Draw();
                }
                else
                {
                    if (lighting)
                    {
                        sideSprite.Color = System.Drawing.Color.Black;
                    }
                    else
                    {
                        sideSprite.Color = System.Drawing.Color.White;
                    }
                    sideSprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                    sideSprite.Draw();
                }
            }
        }

        public override void RenderTop(float xTopLeft, float yTopLeft, int tileSpacing, bool lighting)
        {
            if (Visible)
            {
                sprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                sprite.Position -= new Vector2D(0, tileSpacing);
                if (lights.Count > 0)
                {
                    if (lighting)
                    {
                        System.Drawing.Color col = System.Drawing.Color.Transparent;
                        foreach (Atom.Light l in lights)
                        {
                            col = Blend(col, l.color, 0.5d);
                        }
                        sprite.Color = col;
                    }
                    else
                    {
                        sprite.Color = System.Drawing.Color.White;
                    }
                    sprite.Draw();
                }
                else
                {
                    if (lighting)
                    {
                        sprite.Color = System.Drawing.Color.Black;
                    }
                    else
                    {
                        sprite.Color = System.Drawing.Color.White;
                    }
                    sprite.Draw();
                }
            }
        }
    }
}
