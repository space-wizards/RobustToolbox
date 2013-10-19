using System;
using System.Drawing;
using ClientInterfaces.Collision;
using ClientInterfaces.Map;
using GameObject;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13.IoC;
using SS13_Shared;

namespace ClientServices.Tiles
{
    public class Wall : Tile, ICollidable
    {
        private readonly IMapManager mapMgr;
        private readonly Sprite plainWall;
        private readonly Sprite wallCorner1;
        private readonly Sprite wallCorner2;

        public Wall(TileState state, Vector2D position)
            : base(state, position)
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

        public bool IsHardCollidable
        {
            get { return true; }
        }

        public RectangleF AABB
        {
            get { return new RectangleF(Position, Sprite.Size); }
        }

        public void Bump(Entity collider)
        {
        }

        #endregion

        public override void Render(float xTopLeft, float yTopLeft, int tileSpacing, Batch batch)
        {
            surroundDirs = mapMgr.SetSprite(Position); //Optimize.

            if (surroundDirs == 3 ||
                surroundDirs == 2 &&
                !(surroundingTiles[2] != null && surroundingTiles[2].surroundingTiles[3] != null &&
                  surroundingTiles[2].surroundingTiles[3].ConnectSprite)) //north and east
                sideSprite = wallCorner1;

            else if (surroundDirs == 9 ||
                     surroundDirs == 8 &&
                     !(surroundingTiles[2] != null && surroundingTiles[2].surroundingTiles[1] != null &&
                       surroundingTiles[2].surroundingTiles[1].ConnectSprite)) //north and west 
                sideSprite = wallCorner2;
            else
                sideSprite = plainWall;
            if (((surroundDirs & 4) == 0))
            {
                sideSprite.SetPosition((float) Position.X - xTopLeft,
                                       (float) Position.Y - yTopLeft);
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
            switch (from)
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
            switch (d)
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

            Gorgon.CurrentRenderTarget.FilledRectangle(drawX + bx, drawY + bx, width + Math.Abs(bx),
                                                       height + Math.Abs(by), Color.Black);
        }

        public override void RenderPos(float x, float y, int tileSpacing, int lightSize)
        {
            //Not drawing occlusion for tiles on the edge. Fuck this. Looks better too since there isnt actually anything to hide behind them.
            if ((Position.X == ((mapMgr.GetMapWidth() - 1) * mapMgr.GetTileSpacing()) || Position.X == 0) ||
                (Position.Y == ((mapMgr.GetMapHeight() - 1) * mapMgr.GetTileSpacing()) || Position.Y == 0))
                return;

            int l = lightSize/2;
            var from = Direction.East;
            if (l < x && l < y)
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

            if (l < x)
            {
                if (!IsOpaque(1) || (IsOpaque(1) && IsOpaque(2)))
                    RenderOccluder(Direction.East, from, x, y, tileSpacing);

                if (surroundingTiles[2] != null && surroundingTiles[2].surroundingTiles[3] != null && surroundingTiles[2].surroundingTiles[3].Opaque && !IsOpaque(3))
                    RenderOccluder(Direction.West, from, x, y, tileSpacing);

                if (l < y)
                {
                    if (!IsOpaque(2) || (IsOpaque(2) && !IsOpaque(0)))
                    {
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    }
                    if (!IsOpaque(2))
                        RenderOccluder(Direction.West, from, x, y, tileSpacing);
                }
                else if (l > y + tileSpacing)
                {
                    if (!IsOpaque(0) || (IsOpaque(0) && !IsOpaque(2) &&
                         (l < x + tileSpacing && !IsOpaque(1))))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    if (IsOpaque(1) && IsOpaque(3))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
                else if (l >= y && l <= y + tileSpacing)
                {
                    if (!IsOpaque(2) || (IsOpaque(2) && !IsOpaque(0)))
                    {
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    }
                    if (!IsOpaque(2))
                        RenderOccluder(Direction.West, from, x, y, tileSpacing);

                    if (!IsOpaque(0) || (IsOpaque(0) && !IsOpaque(2)))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
            }
            else if (l > x + tileSpacing)
            {
                if (!IsOpaque(3) || (IsOpaque(3) && IsOpaque(2)))
                    RenderOccluder(Direction.West, from, x, y, tileSpacing);
                if (surroundingTiles[2] != null && surroundingTiles[2].surroundingTiles[1] != null &&
                    surroundingTiles[2].surroundingTiles[1].Opaque && !IsOpaque(1))
                    RenderOccluder(Direction.East, from, x, y, tileSpacing);

                if (l < y)
                {
                    if (!IsOpaque(2) || (IsOpaque(2) && !IsOpaque(0)))
                    {
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    }
                    if (!IsOpaque(2))
                        RenderOccluder(Direction.East, from, x, y, tileSpacing);
                }
                else if (l > y + tileSpacing)
                {
                    if (!IsOpaque(0) ||
                        (IsOpaque(0) && !IsOpaque(2) &&
                         (l < x + tileSpacing && !IsOpaque(1))))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    if (IsOpaque(1))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
                else if (l >= y && l <= y + tileSpacing)
                {
                    if (!IsOpaque(2) || (IsOpaque(2) && !IsOpaque(0)))
                    {
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    }
                    if (!IsOpaque(2))
                        RenderOccluder(Direction.East, from, x, y, tileSpacing);
                    if (!IsOpaque(0) || (IsOpaque(0) && !IsOpaque(2)))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
            }
            else if (l >= x && l <= x + tileSpacing)
            {
                if (!IsOpaque(1) || (IsOpaque(1) && IsOpaque(2)))
                    RenderOccluder(Direction.East, from, x, y, tileSpacing);
                if (!IsOpaque(3) || (IsOpaque(3) && IsOpaque(2)))
                    RenderOccluder(Direction.West, from, x, y, tileSpacing);

                if (l < y)
                {
                    if (!IsOpaque(2) || (IsOpaque(2) && !IsOpaque(0)))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
                else if (l > y + tileSpacing)
                {
                    if (!IsOpaque(0) ||
                        (IsOpaque(0) && !IsOpaque(2) &&
                         (l < x + tileSpacing && !IsOpaque(1))))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
                else if (l >= y && l <= y + tileSpacing)
                {
                    if (!IsOpaque(2) || (IsOpaque(2) && !IsOpaque(0)))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                    if (!IsOpaque(0) || (IsOpaque(0) && !IsOpaque(2)))
                        RenderOccluder(Direction.North, from, x, y, tileSpacing);
                }
            }
        }

        private bool IsOpaque(int i)
        {
            if (surroundingTiles[i] != null && surroundingTiles[i].Opaque)
                return true;
            return false;
        }

        public override void RenderPosOffset(float x, float y, int tileSpacing, Vector2D lightPosition)
        {
            Vector2D lightVec = lightPosition - new Vector2D(x + tileSpacing/2.0f, y + tileSpacing/2.0f);
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

            if (surroundingTiles[0] != null && IsOpaque(0) && lightVec.Y < 0) // tile to north
                lightVec.Y = 2;
            if (surroundingTiles[1] != null && IsOpaque(1) && lightVec.X > 0)
                lightVec.X = -2;
            if (surroundingTiles[2] != null && IsOpaque(2) && lightVec.Y > 0)
                lightVec.Y = -2;
            if (surroundingTiles[3] != null && IsOpaque(3) && lightVec.X < 0)
                lightVec.X = 2;

            Gorgon.CurrentRenderTarget.FilledRectangle(x + lightVec.X, y + lightVec.Y, sideSprite.Width + 1,
                                                       sideSprite.Height + 1, Color.FromArgb(0, Color.Transparent));
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
            Sprite =
                _resourceManager.GetSprite("wall_texture" + mapMgr.SetSprite(Position).ToString());
            //Optimize

            Sprite.SetPosition(Position.X - xTopLeft, Position.Y - yTopLeft);
            Sprite.Position -= new Vector2D(0, tileSpacing);
            Sprite.Color = Color.FromArgb(200, Color.White);

            wallTopsBatch.AddClone(Sprite);
        }
    }
}