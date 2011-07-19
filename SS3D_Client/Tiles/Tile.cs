using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using System.Drawing;
using SS3D.Modules;

namespace SS3D.Tiles
{
    public abstract class Tile
    {
        public TileType tileType = TileType.None;
        public TileState tileState = TileState.Healthy;
        public string name;
        public Sprite sprite;
        public Sprite sideSprite;
        public Vector2D position;
        public Point tilePosition;
        public bool Visible = false;
        public bool sightBlocked = false; // Is something on this tile that blocks sight through it, like a door (used for lighting)
        public byte surroundDirs = 0;
        public Tile[] surroundingTiles;

        public Tile(Sprite _sprite, TileState state, float size, Vector2D _position, Point _tilePosition)
        {
            tileState = state;
            position = _position;
            tilePosition = _tilePosition;
            sprite = _sprite;
            sprite.SetPosition(_position.X, _position.Y);
            sightBlocked = false;
            surroundingTiles = new Tile[4];
        }

        public Tile(Sprite _sprite, Sprite _side, TileState state, float size, Vector2D _position, Point _tilePosition)
        {
            tileState = state;
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

        public virtual void Render(float xTopLeft, float yTopLeft, int tileSpacing, List<Light> lights)
        {
            if (Visible)
            {
                sprite.Color = Color.White;
                sprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                LightManager.Singleton.ApplyLightsToSprite(lights, sprite, new Vector2D(xTopLeft, yTopLeft));
                sprite.Draw();
            }
        }

        public virtual void RenderTop(float xTopLeft, float yTopLeft, int tileSpacing, List<Light> lights)
        {
        }
    }
}