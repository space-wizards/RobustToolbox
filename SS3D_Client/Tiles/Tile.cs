using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using System.Drawing;

namespace SS3D.Tiles
{
    public abstract class Tile
    {
        public TileType tileType = TileType.None;
        public string name;
        public Sprite sprite;
        public Sprite sideSprite;
        public Vector2D position;
        public Point tilePosition;
        public bool Visible = false;
        public bool sightBlocked = false; // Is something on this tile that blocks sight through it, like a door (used for lighting)
        public List<Atom.Light> lights;
        public byte surroundDirs = 0;

        public Tile(Sprite _sprite, float size, Vector2D _position, Point _tilePosition)
        {
            lights = new List<Atom.Light>();
            position = _position;
            tilePosition = _tilePosition;
            sprite = _sprite;
            sprite.SetPosition(_position.X, _position.Y);
            sightBlocked = false;
        }

        public Tile(Sprite _sprite, Sprite _side, float size, Vector2D _position, Point _tilePosition)
        {
            lights = new List<Atom.Light>();
            position = _position;
            tilePosition = _tilePosition;
            sprite = _sprite;
            sprite.SetPosition(_position.X, _position.Y);

            sideSprite = _side;
            sideSprite.SetPosition(_position.X, _position.Y);
            sightBlocked = false;
        }

        public void SetSprites(Sprite _sprite, Sprite _side, byte _surroundDirs)
        {
            sprite = _sprite;
            sideSprite = _side;
            surroundDirs = _surroundDirs;
        }

        public virtual void Render(float xTopLeft, float yTopLeft, int tileSpacing, bool lighting)
        {
            if (Visible)
            {
                sprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                if (lights.Count > 0)
                {
                    if (lighting)
                    {
                        System.Drawing.Color col = System.Drawing.Color.Black;
                        foreach (Atom.Light l in lights)
                        {
                            double d = 1;
                            Point p = new Point(tilePosition.X - l.position.X, tilePosition.Y - l.position.Y);
                            p.X *= p.X;
                            p.Y *= p.Y;
                            d = Math.Sqrt(p.X + p.Y);
                            if (d < 2)
                                d = 2;
                            col = Blend(col, l.color, 1 / d);
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
                    if(lighting)
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

        public virtual void RenderTop(float xTopLeft, float yTopLeft, int tileSpacing, bool lighting)
        {
        }

        // This definately shouldn't be here but i'm putting it here for now just so it works.
        public System.Drawing.Color Blend(System.Drawing.Color color, System.Drawing.Color backColor, double amount)
        {
            byte r = (byte)((color.R * amount) + (backColor.R * amount));
            byte g = (byte)((color.G * amount) + (backColor.G * amount));
            byte b = (byte)((color.B * amount) + (backColor.B * amount));
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        public System.Drawing.Color Add(System.Drawing.Color color, System.Drawing.Color color2)
        {
            byte r = (byte)Math.Max((color.R + color2.R), 255);
            byte g = (byte)Math.Max((color.G + color2.G), 255);
            byte b = (byte)Math.Max((color.B + color2.B), 255);
            return System.Drawing.Color.FromArgb(r, g, b);
        }

    }
}
