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
        private Sprite plainWall;
        private Sprite wallCorner1;
        private Sprite wallCorner2;
        public Wall(Sprite _sprite, Sprite _side, TileState state, float size, Vector2D _position, Point _tilePosition)
            : base(_sprite, _side, state, size, _position, _tilePosition)
        {
            tileType = TileType.Wall;
            name = "Wall";
            sprite = _sprite;
            sideSprite = _side;
            plainWall = ResMgr.Singleton.GetSprite("WallSide");
            wallCorner1 = ResMgr.Singleton.GetSprite("wallcorner");
            wallCorner2 = ResMgr.Singleton.GetSprite("wallcorner2");
        }

        public override void Render(float xTopLeft, float yTopLeft, int tileSpacing)
        {
            if (surroundDirs == 3 || surroundDirs == 2) //north and east
                sideSprite = wallCorner1;
            else if (surroundDirs == 9 || surroundDirs == 8) // north and west
                sideSprite = wallCorner2;
            else
                sideSprite = plainWall;
            if (Visible && ((surroundDirs&4) == 0))
            {
                sideSprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                sideSprite.Color = Color.White;
                sideSprite.Draw();
            }
        }

        public override void DrawDecals(float xTopLeft, float yTopLeft, int tileSpacing, Batch decalBatch)
        {
            if ((surroundDirs & 4) == 0)
            {
                foreach (TileDecal d in decals)
                {
                    d.Draw(xTopLeft, yTopLeft, tileSpacing, decalBatch);
                }
            }
        }

        public override void RenderTop(float xTopLeft, float yTopLeft, int tileSpacing, Batch wallTopsBatch)
        {
            if (Visible)
            {
                
                sprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                sprite.Position -= new Vector2D(0, tileSpacing);
                sprite.Color = Color.FromArgb(200, Color.White);
                wallTopsBatch.AddClone(sprite);
            }
            else 
            {
                if (surroundingTiles[0].Visible) //if the tile directly north of this one is visible, we should draw the wall top for this tile.
                {
                    sprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                    sprite.Position -= new Vector2D(0, tileSpacing);
                    sprite.Color = Color.FromArgb(200, Color.White);
                    wallTopsBatch.AddClone(sprite);
                }
            }
        }
    }
}
