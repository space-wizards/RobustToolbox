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
        public Tile[] surroundingTiles;
        public Color color;

        public Tile(Sprite _sprite, float size, Vector2D _position, Point _tilePosition)
        {
            lights = new List<Atom.Light>();
            position = _position;
            tilePosition = _tilePosition;
            sprite = _sprite;
            sprite.SetPosition(_position.X, _position.Y);
            sightBlocked = false;
            surroundingTiles = new Tile[4];
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
            surroundingTiles = new Tile[4];
        }

        public void SetSprites(Sprite _sprite, Sprite _side, byte _surroundDirs)
        {
            sprite = _sprite;
            sideSprite = _side;
            surroundDirs = _surroundDirs;
        }

        // See bottom of file for lighting description.
        public void DoColour()
        {
            if (Visible)
            {
                if (lights.Count > 0)
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
                        col = Add(col, l.color, 1 / d);
                    }
                    color = col;
                }
                else
                {
                    color = Color.FromArgb(15, 15, 15);
                }
            }
            else
            {
                color = Color.Black;
            }
        }

        public virtual void Render(float xTopLeft, float yTopLeft, int tileSpacing, bool lighting)
        {
            if (Visible)
            {
                sprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                sprite.Color = color;
                ShadeCorners(sprite);
                sprite.Draw();
            }
        }
    
        public virtual void RenderTop(float xTopLeft, float yTopLeft, int tileSpacing, bool lighting)
        {
        }

        // This definately shouldn't be here but i'm putting it here for now just so it works.
        public System.Drawing.Color Blend(System.Drawing.Color color, System.Drawing.Color backColor, double amount)
        {
            byte r = Math.Min((byte)255, (byte)((color.R * amount) + (backColor.R * amount)));
            byte g = Math.Min((byte)255, (byte)((color.G * amount) + (backColor.G * amount)));
            byte b = Math.Min((byte)255, (byte)((color.B * amount) + (backColor.B * amount)));
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        public System.Drawing.Color Add(System.Drawing.Color color, System.Drawing.Color color2, double amount)
        {
            byte r = Math.Min((byte)255, (byte)(color.R + (color2.R * amount)));
            byte g = Math.Min((byte)255, (byte)(color.G + (color2.G * amount)));
            byte b = Math.Min((byte)255, (byte)(color.B + (color2.B * amount)));
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        public void ShadeCorners(Sprite _sprite)
        {
            if (surroundingTiles[0] != null && surroundingTiles[1] != null)
            {
                _sprite.SetSpriteVertexColor(VertexLocations.UpperRight, Blend(color, Blend(surroundingTiles[0].color, surroundingTiles[1].color, 0.5), 0.5));
            }
            else
            {
                _sprite.SetSpriteVertexColor(VertexLocations.UpperRight, Color.Black);
            }
            if (surroundingTiles[1] != null && surroundingTiles[2] != null)
            {
                _sprite.SetSpriteVertexColor(VertexLocations.LowerRight, Blend(color, Blend(surroundingTiles[1].color, surroundingTiles[2].color, 0.5), 0.5));
            }
            else
            {
                _sprite.SetSpriteVertexColor(VertexLocations.LowerRight, Color.Black);
            }
            if (surroundingTiles[2] != null && surroundingTiles[3] != null)
            {
                _sprite.SetSpriteVertexColor(VertexLocations.LowerLeft, Blend(color, Blend(surroundingTiles[2].color, surroundingTiles[3].color, 0.5), 0.5));
            }
            else
            {
                _sprite.SetSpriteVertexColor(VertexLocations.LowerLeft, Color.Black);
            }
            if (surroundingTiles[3] != null && surroundingTiles[0] != null)
            {
                _sprite.SetSpriteVertexColor(VertexLocations.UpperLeft, Blend(color, Blend(surroundingTiles[3].color, surroundingTiles[0].color, 0.5), 0.5));
            }
            else
            {
                _sprite.SetSpriteVertexColor(VertexLocations.UpperLeft, Color.Black);
            }

        }
    }
}


/*
 * How the lighting works:
 * Each tile stores a color on it, which is the colour it is lit to by all lights affecting it.
 * This is set by the method DoColour(), and basically just adds together the brightness
 * (modified by distance) of all lights which affect it.
 * This is done BEFORE any tile is rendered.
 * When it comes to rendering time, we set our colour to the one we calculated in DoColour(),
 * and then blend each corner of the tile with a combination of the colours from the two tiles
 * next to the corner, so the NW corner would have a combination of the colours from the N and the
 * W tiles.
 * 
 * We do it this way round as the problem with calculating the colours at render-time is that we cannot 
 * check the colour of the tiles below or to the right of us, as they haven't been processed yet, so their 
 * colour value hasn't been updated yet.
*/