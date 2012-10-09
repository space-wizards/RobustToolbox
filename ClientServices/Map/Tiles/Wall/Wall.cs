using System;
using System.Drawing;
using ClientInterfaces.Collision;
using ClientInterfaces.Lighting;
using ClientInterfaces.Resource;
using ClientInterfaces.Map;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using ClientInterfaces;
using SS13_Shared;
using ClientInterfaces.GOC;
using ClientServices.Resources;
using SS13.IoC;

namespace ClientServices.Tiles
{
    public class Wall : Tile, ICollidable
    {
        private Sprite plainWall;
        private Sprite wallCorner1;
        private Sprite wallCorner2;

        private IMapManager mapMgr;

        public bool IsHardCollidable
        {
            get { return true; }
        }

        public Wall(TileState state, Vector2D position, Point tilePosition)
            : base(state, position, tilePosition)
        {
            ConnectSprite = true;
            Opaque = true;

            name = "Wall";

            Sprite = _resourceManager.GetSprite("wall_texture0");
            sideSprite = _resourceManager.GetSprite("wall_side");

            plainWall = _resourceManager.GetSprite("wall_side");
            wallCorner1 = _resourceManager.GetSprite("wall_corner");
            wallCorner2 = _resourceManager.GetSprite("wall_corner2");

            mapMgr = IoCManager.Resolve<IMapManager>();
        }

        #region ICollidable Members
        public RectangleF AABB
        {
            get
            {
                return new RectangleF(Position, Sprite.Size);
            }
        }

        public void Bump(IEntity collider)
        { }
        #endregion 

        public override void Render(float xTopLeft, float yTopLeft, int tileSpacing, Batch batch)
        {
            surroundDirs = mapMgr.SetSprite(TilePosition.X, TilePosition.Y); //Optimize.

            if (surroundDirs == 3 || surroundDirs == 2 && !(surroundingTiles[2] != null && surroundingTiles[2].surroundingTiles[3]!= null && surroundingTiles[2].surroundingTiles[3].ConnectSprite)) //north and east
                sideSprite = wallCorner1;

            else if (surroundDirs == 9 || surroundDirs == 8 && !(surroundingTiles[2] != null && surroundingTiles[2].surroundingTiles[1] != null && surroundingTiles[2].surroundingTiles[1].ConnectSprite)) //north and west 
                sideSprite = wallCorner2;
            else
                sideSprite = plainWall;
            if (((surroundDirs&4) == 0))
            {
                sideSprite.SetPosition((float)TilePosition.X * tileSpacing - xTopLeft, (float)TilePosition.Y * tileSpacing - yTopLeft);
                sideSprite.Color = Color.White;
                batch.AddClone(sideSprite);
            }
        }

        private void RenderOccluder(Direction d, Direction from, float x, float y, int tileSpacing)
        {
            int bx = 0;
            int by = 0;
            float drawX = 0;
            float drawY = 0;
            int width = 0;
            int height = 0;
            switch(from)
            {
                case Direction.North:
                    by = -2;
                    break;
                case Direction.NorthEast:
                    by = -2;
                    bx = 2;
                    break;
                case Direction.East:
                    bx = 2;
                    break;
                case Direction.SouthEast:
                    by = 2;
                    bx = 2;
                    break;
                case Direction.South:
                    by = 2;
                    break;
                case Direction.SouthWest:
                    by = 2;
                    bx = -2;
                    break;
                case Direction.West:
                    bx = -2;
                    break;
                case Direction.NorthWest:
                    bx = -2;
                    by = -2;
                    break;
            }
            switch(d)
            {
                case Direction.North:
                    drawX = x;
                    drawY = y;
                    width = tileSpacing;
                    height = 2;
                    break;
                case Direction.East:
                    drawX = x + tileSpacing;
                    drawY = y;
                    width = 2;
                    height = tileSpacing;
                    break;
                case Direction.South:
                    drawX = x;
                    drawY = y + tileSpacing;
                    width = tileSpacing;
                    height = 2;
                    break;
                case Direction.West:
                    drawX = x;
                    drawY = y;
                    width = 2;
                    height = tileSpacing;
                    break;
            }

            Gorgon.CurrentRenderTarget.FilledRectangle(drawX + bx, drawY + bx, width + Math.Abs(bx), height + Math.Abs(by), Color.Black);
        }

        public override void RenderPos(float x, float y, int tileSpacing, int lightSize)
        {
            int l = lightSize/2;
            Direction from = Direction.East;
            if(l < x && l < y)
                from = Direction.NorthWest;
            else if (l > x + tileSpacing && l < y)
                from = Direction.NorthEast;
            else if (l < x && l > y + tileSpacing)
                from = Direction.SouthWest;
            else if (l > x + tileSpacing && l > y + tileSpacing)
                from = Direction.SouthEast;
            else if (l < x)
                from = Direction.West;
            else if (l > x + tileSpacing)
                from = Direction.East;
            else if (l < y)
                from = Direction.North;
            else if (l > y + tileSpacing)
                from = Direction.South;
            
            if(l < x)
            {
                if (!surroundingTiles[1].Opaque || (surroundingTiles[1].Opaque && surroundingTiles[2].Opaque))
                    RenderOccluder(Direction.East, from, x, y, tileSpacing);
                if (surroundingTiles[2].surroundingTiles[3].Opaque && !surroundingTiles[3].Opaque)
                    RenderOccluder(Direction.West, from, x, y, tileSpacing);

                if(l < y)
                {
                    if (!surroundingTiles[2].Opaque || (surroundingTiles[2].Opaque && !surroundingTiles[0].Opaque))
                    {
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    }
                    if (!surroundingTiles[2].Opaque)
                        RenderOccluder(Direction.West, from, x, y, tileSpacing);
                }
                else if (l > y + tileSpacing)
                {
                    if (!surroundingTiles[0].Opaque || (surroundingTiles[0].Opaque && !surroundingTiles[2].Opaque && (l < x + tileSpacing && !surroundingTiles[1].Opaque)))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    if (surroundingTiles[1].Opaque && surroundingTiles[3].Opaque)
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
                else if (l >= y && l <= y + tileSpacing)
                {
                    if (!surroundingTiles[2].Opaque || (surroundingTiles[2].Opaque && !surroundingTiles[0].Opaque))
                    {
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    }
                    if (!surroundingTiles[2].Opaque)
                        RenderOccluder(Direction.West, from, x, y, tileSpacing);

                    if (!surroundingTiles[0].Opaque || (surroundingTiles[0].Opaque && !surroundingTiles[2].Opaque))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
            }
            else if (l > x + tileSpacing)
            {
                if (!surroundingTiles[3].Opaque || (surroundingTiles[3].Opaque && surroundingTiles[2].Opaque))
                    RenderOccluder(Direction.West, from, x, y, tileSpacing);
                if (surroundingTiles[2].surroundingTiles[1].Opaque && !surroundingTiles[1].Opaque)
                    RenderOccluder(Direction.East, from, x, y, tileSpacing);

                if (l < y)
                {
                    if (!surroundingTiles[2].Opaque || (surroundingTiles[2].Opaque && !surroundingTiles[0].Opaque))
                    {
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    }
                    if (!surroundingTiles[2].Opaque)
                        RenderOccluder(Direction.East, from, x, y, tileSpacing);
                }
                else if (l > y + tileSpacing)
                {
                    if (!surroundingTiles[0].Opaque || (surroundingTiles[0].Opaque && !surroundingTiles[2].Opaque && (l < x + tileSpacing && !surroundingTiles[1].Opaque)))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    if (surroundingTiles[1].Opaque)
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
                else if (l >= y && l <= y + tileSpacing)
                {
                    if (!surroundingTiles[2].Opaque || (surroundingTiles[2].Opaque && !surroundingTiles[0].Opaque))
                    {
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    }
                    if (!surroundingTiles[2].Opaque)
                        RenderOccluder(Direction.East, from, x, y, tileSpacing);
                    if (!surroundingTiles[0].Opaque || (surroundingTiles[0].Opaque && !surroundingTiles[2].Opaque))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
            }
            else if (l >= x && l <= x + tileSpacing)
            {
                if (!surroundingTiles[1].Opaque || (surroundingTiles[1].Opaque && surroundingTiles[2].Opaque))
                    RenderOccluder(Direction.East, from, x, y, tileSpacing);
                if (!surroundingTiles[3].Opaque || (surroundingTiles[3].Opaque && surroundingTiles[2].Opaque))
                    RenderOccluder(Direction.West, from, x, y, tileSpacing);

                if (l < y)
                {
                    if (!surroundingTiles[2].Opaque || (surroundingTiles[2].Opaque && !surroundingTiles[0].Opaque))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
                else if (l > y + tileSpacing)
                {
                    if (!surroundingTiles[0].Opaque || (surroundingTiles[0].Opaque && !surroundingTiles[2].Opaque && (l < x + tileSpacing && !surroundingTiles[1].Opaque)))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
                else if (l >= y && l <= y + tileSpacing)
                {
                    if (!surroundingTiles[2].Opaque || (surroundingTiles[2].Opaque && !surroundingTiles[0].Opaque))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    if (!surroundingTiles[0].Opaque || (surroundingTiles[0].Opaque && !surroundingTiles[2].Opaque))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
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

            if (surroundingTiles[0] != null && surroundingTiles[0].Opaque && lightVec.Y < 0) // tile to north
                lightVec.Y = 2;
            if (surroundingTiles[1] != null && surroundingTiles[1].Opaque && lightVec.X > 0)
                lightVec.X = -2;
            if (surroundingTiles[2] != null && surroundingTiles[2].Opaque && lightVec.Y > 0)
                lightVec.Y = -2;
            if (surroundingTiles[3] != null && surroundingTiles[3].Opaque && lightVec.X < 0)
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
            Sprite = _resourceManager.GetSprite("wall_texture" + mapMgr.SetSprite(TilePosition.X, TilePosition.Y).ToString()); //Optimize

            Sprite.SetPosition(TilePosition.X * tileSpacing - xTopLeft, TilePosition.Y * tileSpacing - yTopLeft);
            Sprite.Position -= new Vector2D(0, tileSpacing);
            Sprite.Color = Color.FromArgb(200, Color.White);

            wallTopsBatch.AddClone(Sprite);
        }
    }
}
