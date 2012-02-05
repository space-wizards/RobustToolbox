using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientServices.Collision;
using ClientServices.Resources;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using ClientInterfaces;
using ClientServices;
using SS13_Shared;

namespace ClientServices.Map.Tiles.Wall
{
    public class Wall : Tile, ICollidable
    {
        private Sprite plainWall;
        private Sprite wallCorner1;
        private Sprite wallCorner2;

        public bool IsHardCollidable
        {
            get { return true; }
        }

        public Wall(Sprite _sprite, Sprite _side, TileState state, float size, Vector2D _position, Point _tilePosition, ILightManager _lightManager, ResourceManager resourceManager)
            : base(_sprite, _side, state, size, _position, _tilePosition, _lightManager, resourceManager)
        {
            tileType = TileType.Wall;
            name = "Wall";
            sprite = _sprite;
            sideSprite = _side;

            plainWall = resourceManager.GetSprite("wall_side");
            wallCorner1 = resourceManager.GetSprite("wall_corner");
            wallCorner2 = resourceManager.GetSprite("wall_corner2");
        }

        #region ICollidable Members
        public RectangleF AABB
        {
            get
            {
                return new RectangleF(position, sprite.Size);
            }
        }

        public void Bump()
        { }
        #endregion 

        public override void Initialize()
        {
            base.Initialize();

            var collisionManager = ServiceManager.Singleton.GetService<CollisionManager>();
            if (collisionManager != null)
                collisionManager.AddCollidable(this);
        }

        public override void Render(float xTopLeft, float yTopLeft, int tileSpacing)
        {
            if (surroundDirs == 3 || surroundDirs == 2 && !(surroundingTiles[2] != null && surroundingTiles[2].surroundingTiles[3]!= null && surroundingTiles[2].surroundingTiles[3].tileType == TileType.Wall)) //north and east
                sideSprite = wallCorner1;
            else if (surroundDirs == 9 || surroundDirs == 8 && !(surroundingTiles[2] != null && surroundingTiles[2].surroundingTiles[1] != null && surroundingTiles[2].surroundingTiles[1].tileType == TileType.Wall)) //north and west 
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
