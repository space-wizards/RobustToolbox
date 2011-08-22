using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS3D.Modules;

namespace SS3D.Tiles.Wall
{
    public class Wall : Tile
    {

        public Wall(Sprite _sprite, Sprite _side, TileState state, float size, Vector2D _position, Point _tilePosition)
            : base(_sprite, _side, state, size, _position, _tilePosition)
        {
            tileType = TileType.Wall;
            name = "Wall";
            sprite = _sprite;
            sideSprite = _side;
        }

        public override void Render(float xTopLeft, float yTopLeft, int tileSpacing)
        {
            if (Visible && ((surroundDirs&4) == 0))
            {
                sideSprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                sideSprite.Color = Color.White;
                LightManager.Singleton.ApplyLightsToSprite(tileLights, sideSprite, new Vector2D(xTopLeft, yTopLeft));
                sideSprite.Draw();
            }
        }

        public override void RenderTop(float xTopLeft, float yTopLeft, int tileSpacing)
        {
            if (Visible)
            {
                sprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                sprite.Position -= new Vector2D(0, tileSpacing);
                sprite.Color = Color.White;
                LightManager.Singleton.ApplyLightsToSprite(tileLights, sprite, new Vector2D(xTopLeft, yTopLeft));
                sprite.Draw();
            }
        }
    }
}
