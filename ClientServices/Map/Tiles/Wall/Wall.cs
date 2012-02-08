using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using ClientInterfaces;
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

        public Wall(Sprite _sprite, Sprite _side, TileState state, float size, Vector2D _position, Point _tilePosition, ILightManager _lightManager, IResourceManager resourceManager)
            : base(_sprite, _side, state, size, _position, _tilePosition, _lightManager, resourceManager)
        {
            TileType = TileType.Wall;
            name = "Wall";
            Sprite = _sprite;
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
                return new RectangleF(Position, Sprite.Size);
            }
        }

        public void Bump()
        { }
        #endregion 

        public override void Render(float xTopLeft, float yTopLeft, int tileSpacing)
        {
            if (surroundDirs == 3 || surroundDirs == 2 && !(surroundingTiles[2] != null && surroundingTiles[2].surroundingTiles[3]!= null && surroundingTiles[2].surroundingTiles[3].TileType == TileType.Wall)) //north and east
                sideSprite = wallCorner1;
            else if (surroundDirs == 9 || surroundDirs == 8 && !(surroundingTiles[2] != null && surroundingTiles[2].surroundingTiles[1] != null && surroundingTiles[2].surroundingTiles[1].TileType == TileType.Wall)) //north and west 
                sideSprite = wallCorner2;
            else
                sideSprite = plainWall;
            if (Visible && ((surroundDirs&4) == 0))
            {
                sideSprite.SetPosition(TilePosition.X * tileSpacing - xTopLeft, TilePosition.Y * tileSpacing - yTopLeft);
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
                
                Sprite.SetPosition(TilePosition.X * tileSpacing - xTopLeft, TilePosition.Y * tileSpacing - yTopLeft);
                Sprite.Position -= new Vector2D(0, tileSpacing);
                Sprite.Color = Color.FromArgb(200, Color.White);
                wallTopsBatch.AddClone(Sprite);
            }
            else 
            {
                if (surroundingTiles[0].Visible) //if the tile directly north of this one is visible, we should draw the wall top for this tile.
                {
                    Sprite.SetPosition(TilePosition.X * tileSpacing - xTopLeft, TilePosition.Y * tileSpacing - yTopLeft);
                    Sprite.Position -= new Vector2D(0, tileSpacing);
                    Sprite.Color = Color.FromArgb(200, Color.White);
                    wallTopsBatch.AddClone(Sprite);
                }
            }
        }
    }
}
