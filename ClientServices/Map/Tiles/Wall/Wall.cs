using System.Drawing;
using ClientInterfaces.Collision;
using ClientInterfaces.Lighting;
using ClientInterfaces.Resource;
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
            if (((surroundDirs&4) == 0))
            {
                sideSprite.SetPosition(TilePosition.X * tileSpacing - xTopLeft, TilePosition.Y * tileSpacing - yTopLeft);
                sideSprite.Color = Color.White;
                sideSprite.Draw();
            }
        }

        public override void RenderPos(float x, float y, int tileSpacing, int lightSize)
        {
            if (x > (lightSize / 2))
            {
                if (surroundingTiles[1].TileType == TileType.Floor || (surroundingTiles[1].TileType == TileType.Wall && surroundingTiles[2].TileType == TileType.Wall))
                {
                    Gorgon.CurrentRenderTarget.FilledRectangle(x + tileSpacing, y, 2, tileSpacing, Color.Black);
                }
            }
            else if (x < (lightSize / 2))
            {
                if (surroundingTiles[3].TileType == TileType.Floor || (surroundingTiles[3].TileType == TileType.Wall && surroundingTiles[2].TileType == TileType.Wall))
                {
                    Gorgon.CurrentRenderTarget.FilledRectangle(x, y, 2, tileSpacing, Color.Black);
                }
            }
            if (y > (lightSize / 2))
            {
                if (surroundingTiles[2].TileType == TileType.Floor || (surroundingTiles[2].TileType == TileType.Wall && surroundingTiles[0].TileType == TileType.Floor))
                {
                    Gorgon.CurrentRenderTarget.FilledRectangle(x, y, tileSpacing, 2, Color.Black);
                }
            }
            else if (y < (lightSize / 2))
            {
                if (surroundingTiles[0].TileType == TileType.Floor || (surroundingTiles[0].TileType == TileType.Wall && surroundingTiles[2].TileType == TileType.Floor))
                {
                    Gorgon.CurrentRenderTarget.FilledRectangle(x, y, tileSpacing, 2, Color.Black);
                }
            }
        }

        public override void RenderPosOffset(float x, float y, int tileSpacing, Vector2D lightPosition)
        {
            Vector2D lightVec = lightPosition - new Vector2D(x + tileSpacing / 2.0f, y + tileSpacing / 2.0f);
            lightVec.Normalize();
            lightVec *= 10;
            //sideSprite.Color = Color.Black;
            //sideSprite.SetPosition(x + lightVec.X, y + lightVec.Y);
            //sideSprite.BlendingMode = BlendingModes.Inverted;
            //sideSprite.DestinationBlend = AlphaBlendOperation.SourceAlpha;
            //sideSprite.SourceBlend = AlphaBlendOperation.One;
            //sideSprite.Draw();
            if (lightVec.X < 0)
                lightVec.X = -3;
            if (lightVec.X > 0)
                lightVec.X = 3;
            if (lightVec.Y < 0)
                lightVec.Y = -3;
            if (lightVec.Y > 0)
                lightVec.Y = 3;

            if (surroundingTiles[0] != null && surroundingTiles[0].TileType == TileType.Wall && lightVec.Y < 0) // tile to north
                lightVec.Y = 2;
            if (surroundingTiles[1] != null && surroundingTiles[1].TileType == TileType.Wall && lightVec.X > 0)
                lightVec.X = -2;
            if (surroundingTiles[2] != null && surroundingTiles[2].TileType == TileType.Wall && lightVec.Y > 0)
                lightVec.Y = -2;
            if (surroundingTiles[3] != null && surroundingTiles[3].TileType == TileType.Wall && lightVec.X < 0)
                lightVec.X = 2;

            Gorgon.CurrentRenderTarget.FilledRectangle(x + lightVec.X, y + lightVec.Y, sideSprite.Width + 1, sideSprite.Height + 1, Color.FromArgb(0, Color.Transparent));
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
            Sprite.SetPosition(TilePosition.X * tileSpacing - xTopLeft, TilePosition.Y * tileSpacing - yTopLeft);
            Sprite.Position -= new Vector2D(0, tileSpacing);
            Sprite.Color = Color.FromArgb(200, Color.White);
            wallTopsBatch.AddClone(Sprite);
        }
    }
}
