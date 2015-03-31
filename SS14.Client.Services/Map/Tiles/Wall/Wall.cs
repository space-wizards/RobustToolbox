using SS14.Client.Graphics.CluwneLib;
using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.Collision;
using SS14.Client.Interfaces.Map;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using System.Drawing;
using Color = SFML.Graphics.Color;
using SS14.Shared.Maths;

namespace SS14.Client.Services.Tiles
{
    public class Wall : Tile, ICollidable
    {
        private readonly IMapManager mapMgr;
		public CluwneSprite topSpriteNW;
		public CluwneSprite topSpriteSE;
		public CluwneSprite topSprite;
		public CluwneSprite wallEndE;
		public CluwneSprite wallEndW;

        public Wall(TileState state, RectangleF rect, Direction dir)
            : base(state, rect)
        {
            ConnectSprite = true;
            Opaque = true;
            _dir = dir;
            name = "Wall";

            if (dir == Direction.East)
            {
                Sprite = _resourceManager.GetSprite("wall_EW");
                topSprite = _resourceManager.GetSprite("wall_top_EW");
                topSpriteNW = _resourceManager.GetSprite("wall_top_2");
                topSpriteSE = _resourceManager.GetSprite("wall_top_8");
                wallEndE = _resourceManager.GetSprite("wall_end_e");
                wallEndW = _resourceManager.GetSprite("wall_end_w");
            }
            else
            {
                Sprite = _resourceManager.GetSprite("wall_NS");
                topSprite = _resourceManager.GetSprite("wall_top_NS");
                topSpriteNW = _resourceManager.GetSprite("wall_top_4");
                topSpriteSE = _resourceManager.GetSprite("wall_top_1");
            }

            

            mapMgr = IoCManager.Resolve<IMapManager>();
        }

        public override void Initialize()
        {
            SetSprite();
            base.Initialize();
        }

        public void UpdateSurroundDirs()
        {
            surroundDirsNW = DirectionFlags.None;
            surroundDirsSE = DirectionFlags.None;
            float halfSpacing = mapMgr.GetTileSpacing() / 2f;
            Vector2 checkPos = Position + new Vector2(1f, 1f);
            if (mapMgr.GetWallAt(checkPos + new Vector2(0, -halfSpacing)) != null) // North side
            {
                surroundDirsNW |= DirectionFlags.North;
            }
            if (mapMgr.GetWallAt(checkPos + new Vector2(halfSpacing, 0)) != null) // East side
            {
                surroundDirsNW |= DirectionFlags.East;
            }
            if (mapMgr.GetWallAt(checkPos + new Vector2(0, halfSpacing)) != null) // South side
            {
                surroundDirsNW |= DirectionFlags.South;
            }
            if (mapMgr.GetWallAt(checkPos + new Vector2(-halfSpacing, 0)) != null) // West side, yo
            {
                surroundDirsNW |= DirectionFlags.West;
            }

            checkPos += new Vector2(bounds.Width - 2f, bounds.Height - 2f);
            if (mapMgr.GetWallAt(checkPos + new Vector2(0, -halfSpacing)) != null) // North side
            {
                surroundDirsSE |= DirectionFlags.North;
            }
            if (mapMgr.GetWallAt(checkPos + new Vector2(halfSpacing, 0)) != null) // East side
            {
                surroundDirsSE |= DirectionFlags.East;
            }
            if (mapMgr.GetWallAt(checkPos + new Vector2(0, halfSpacing)) != null) // South side
            {
                surroundDirsSE |= DirectionFlags.South;
            }
            if (mapMgr.GetWallAt(checkPos + new Vector2(-halfSpacing, 0)) != null) // West side, yo
            {
                surroundDirsSE |= DirectionFlags.West;
            }

            //return new Point(surroundDirsNW, surroundDirsSE);
        }

        public bool HasNeighborWall(Direction dir)
        {
            if(_dir == Direction.East)
            {
                if (dir == Direction.East)
                    return surroundDirsSE.HasFlag(DirectionFlags.East);
                if (dir == Direction.West)
                    return surroundDirsNW.HasFlag(DirectionFlags.West);
            }
            else
            {
                if (dir == Direction.North)
                    return surroundDirsNW.HasFlag(DirectionFlags.North);
                if (dir == Direction.South)
                    return surroundDirsSE.HasFlag(DirectionFlags.South);
            }
            return false;
        }

        public override void SetSprite()
        {
            UpdateSurroundDirs();
            int first = 0, second = 0;

            if (_dir == Direction.East)
            {
                if (surroundDirsNW.HasFlag(DirectionFlags.West))  first = 1;
                if (surroundDirsSE.HasFlag(DirectionFlags.East))  second = 1;
                Sprite = _resourceManager.GetSprite("wall_EW_" + first + "_" + second);
                
                topSpriteNW = _resourceManager.GetSprite("wall_top_" + (byte)surroundDirsNW);
                topSpriteSE = _resourceManager.GetSprite("wall_top_" + (byte)surroundDirsSE);
            }
            else
            {
                Sprite = _resourceManager.GetSprite("wall_NS");
                topSpriteNW = _resourceManager.GetSprite("wall_top_" + (byte)surroundDirsNW);
                topSpriteSE = _resourceManager.GetSprite("wall_top_" + (byte)surroundDirsSE);
            }
        }


        #region ICollidable Members

        public bool IsHardCollidable
        {
            get { return true; }
        }

        

        public RectangleF AABB
        {
            get { return bounds; }
        }

        public void Bump(Entity collider)
        {
        }

        #endregion

        public override void Render(float xTopLeft, float yTopLeft, Batch batch)
        {
            Sprite.SetPosition((float)bounds.X - xTopLeft,
                                        (float)bounds.Y - (Sprite.Height - bounds.Height) - yTopLeft);

            Sprite.Color = Color.White;
           // batch.AddClone(Sprite);

            if(_dir == Direction.East)
            {
                if (!surroundDirsNW.HasFlag(DirectionFlags.West))
                {
                    if(surroundDirsNW.HasFlag(DirectionFlags.North))
                    {
                        wallEndW.SetPosition((float)bounds.X - xTopLeft - 12f,
                                        (float)bounds.Y - (Sprite.Height - bounds.Height) - yTopLeft);
                      //  batch.AddClone(wallEndW);
                    }
                }
                if (!surroundDirsSE.HasFlag(DirectionFlags.East))
                {
                    if (surroundDirsSE.HasFlag(DirectionFlags.North))
                    {
                        wallEndE.SetPosition((float)bounds.X - xTopLeft + Sprite.Width,
                                        (float)bounds.Y - (Sprite.Height - bounds.Height) - yTopLeft);
                       // batch.AddClone(wallEndE);
                    }
                }
            }
        }

        private void RenderOccluder(Direction d, Direction from, float x, float y)
        {
            int bx = 0;
            int by = 0;
            float drawX = x;
            float drawY = y;
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

            if (_dir == Direction.East)
            {
                switch (d)
                {
                    case Direction.North:
                        drawY += bounds.Height - Sprite.Height;
                        width = (int)Sprite.Width;
                        height = 1;
                        break;
                    case Direction.East:
                        drawX += bounds.Width;
                        width = 1;
                        if (from != Direction.North && from != Direction.NorthEast)
                        {
                            drawY += bounds.Height - Sprite.Height;
                            height = (int)Sprite.Height;
                        }
                        else
                        {
                            drawY += (bounds.Height * 2) - Sprite.Height;
                            height = (int)(Sprite.Height - bounds.Height);
                        }
                        break;
                    case Direction.South:
                        drawY += (2 * bounds.Height) - Sprite.Height;
                        width = (int)Sprite.Width;
                        height = 1;
                        break;
                    case Direction.West:
                        width = 1;
                        if (from != Direction.North && from != Direction.NorthWest)
                        {
                            drawY += bounds.Height - Sprite.Height;
                            height = (int)Sprite.Height;
                        }
                        else
                        {
                            drawY += (bounds.Height * 2) - Sprite.Height;
                            height = (int)(Sprite.Height - bounds.Height);
                        }
                        break;
                }
            }
            else
            {
                switch (d)
                {
                    case Direction.North:
                        drawY += bounds.Height - Sprite.Height;
                        width = (int)Sprite.Width;
                        height = 1;
                        break;
                    case Direction.East:
                        drawX += bounds.Width;
                        width = 1;
                        if (from != Direction.East && from != Direction.SouthEast && from != Direction.NorthEast)
                        {
                            drawY += bounds.Height - Sprite.Height;
                            height = (int)Sprite.Height;
                        }
                        else
                        {
                            drawY += (bounds.Height * 2) - Sprite.Height;
                            height = (int)(Sprite.Height - bounds.Height);
                        }
                        break;
                    case Direction.South:
                        drawY += (2 * bounds.Height) - Sprite.Height;
                        width = (int)Sprite.Width;
                        height = 1;
                        break;
                    case Direction.West:
                        width = 1;
                        if (from != Direction.West && from != Direction.SouthWest && from != Direction.NorthWest)
                        {
                            drawY += bounds.Height - Sprite.Height;
                            height = (int)Sprite.Height;
                        }
                        else
                        {
                            drawY += (bounds.Height * 2) - Sprite.Height;
                            height = (int)(Sprite.Height - bounds.Height);
                        }
                        break;
                }
            }

            //TODO 
           // CluwneLib.CurrentRenderTarget.FilledRectangle(drawX + bx, drawY + by, width, height, Color.Black);
        }

        public override void RenderPos(float x, float y, int tileSpacing, int lightSize)
        {
            int l = lightSize/2;
            var from = Direction.East;
            if (l < x && l < y - (2.5f * bounds.Height))
                from = Direction.NorthWest;
            else if (l > x + bounds.Width && l < y - (2.5f * bounds.Height))
                from = Direction.NorthEast;
            else if (l < x && l > y)
                from = Direction.SouthWest;
            else if (l > x + bounds.Width && l > y)
                from = Direction.SouthEast;
            else if (l < x)
                from = Direction.West;
            else if (l > x + bounds.Width)
                from = Direction.East;
            else if (l < y - (2.5f * bounds.Height))
                from = Direction.North;
            else if (l > y)
                from = Direction.South;

            //Dirs == north=1, east=2, south=3, west=4

            if (_dir == Direction.East) //East-west wall
            {
                if (l < y - (2.5f * bounds.Height)) //Light is north of wall
                {
                    RenderOccluder(Direction.South, from, x, y);
                    if (!HasNeighborWall(Direction.East))
                        RenderOccluder(Direction.East, from, x, y);
                    if (!HasNeighborWall(Direction.West))
                        RenderOccluder(Direction.West, from, x, y);
                }
                else // Light is south of wall
                {
                    RenderOccluder(Direction.North, from, x, y);
                    if (l < x) //light is south west of wall
                    {
                        if (!HasNeighborWall(Direction.East))
                            RenderOccluder(Direction.East, from, x, y);
                    }
                    else if (l > x + bounds.Width) //light is south east of wall
                    {
                        if (!HasNeighborWall(Direction.West))
                            RenderOccluder(Direction.West, from, x, y);
                    }
                    else if (l >= x && l <= x + bounds.Width) //light is south, within wall X bounds
                    {
                        if (!HasNeighborWall(Direction.East))
                            RenderOccluder(Direction.East, from, x, y);
                        if (!HasNeighborWall(Direction.West))
                            RenderOccluder(Direction.West, from, x, y);
                    }
                }
            }
            else // North-south wall
            {
                if (l < x) //Light is west of wall
                {
                    if(!surroundDirsSE.HasFlag(DirectionFlags.East))
                        RenderOccluder(Direction.East, from, x, y);
                    if (l < y) //light is north west of wall
                    {
                        if (!HasNeighborWall(Direction.South))
                        {
                            RenderOccluder(Direction.West, from, x, y);
                            RenderOccluder(Direction.South, from, x, y);
                        }
                    }
                    else if (l > y) // light is south west of wall
                    {
                        if (!HasNeighborWall(Direction.North))
                            RenderOccluder(Direction.North, from, x, y);
                    }
                }
                else if (l > x + bounds.Width) // Light is east of wall
                {
                    if(!surroundDirsSE.HasFlag(DirectionFlags.West))
                        RenderOccluder(Direction.West, from, x, y);
                    if (l < y) //light is north east of wall
                    {
                        if (!HasNeighborWall(Direction.South))
                        {
                            RenderOccluder(Direction.East, from, x, y);
                            RenderOccluder(Direction.South, from, x, y);
                        }
                    }
                    else if (l > y) //light is south east of wall
                    {
                        if (!HasNeighborWall(Direction.North))
                            RenderOccluder(Direction.North, from, x, y);
                    }
                }
                else if (l >= x && l <= x + bounds.Width) //light is within wall X bounds
                {
                    RenderOccluder(Direction.West, Direction.East, x, y);
                    RenderOccluder(Direction.East, Direction.West, x, y);
                    if (l < y) //light is within wall X bounds, north of wall
                    {
                        if (!HasNeighborWall(Direction.South))
                            RenderOccluder(Direction.South, from, x, y);
                    }
                    else if (l > y + bounds.Height) //Light is within wall X bounds, south of wall
                    {
                        if (!HasNeighborWall(Direction.North))
                            RenderOccluder(Direction.North, from, x, y);
                    }
                }
            }

        }

        private bool IsOpaque(int i)
        {
            /*if (surroundingTiles[i] != null && surroundingTiles[i].Opaque)
                return true;*/
            return false;
        }

        public override void RenderPosOffset(float x, float y, int tileSpacing, Vector2 lightPosition)
        {
            Vector2 lightVec = lightPosition - new Vector2(x + tileSpacing/2.0f, y + tileSpacing/2.0f);
            lightVec.Normalize();
            lightVec *= 10;
            Sprite.Color = Color.Black;
            Sprite.SetPosition(x + lightVec.X, y + lightVec.Y);
           //TODO Sprite stuff
            //Sprite.BlendingMode = BlendingModes.Inverted;
            //Sprite.DestinationBlend = AlphaBlendOperation.SourceAlpha;
            //Sprite.SourceBlend = AlphaBlendOperation.One;
            Sprite.Draw();
            if (lightVec.X < 0)
                lightVec.X = -3;
            if (lightVec.X > 0)
                lightVec.X = 3;
            if (lightVec.Y < 0)
                lightVec.Y = -3;
            if (lightVec.Y > 0)
                lightVec.Y = 3;

            /*if (surroundingTiles[0] != null && IsOpaque(0) && lightVec.Y < 0) // tile to north
                lightVec.Y = 2;
            if (surroundingTiles[1] != null && IsOpaque(1) && lightVec.X > 0)
                lightVec.X = -2;
            if (surroundingTiles[2] != null && IsOpaque(2) && lightVec.Y > 0)
                lightVec.Y = -2;
            if (surroundingTiles[3] != null && IsOpaque(3) && lightVec.X < 0)
                lightVec.X = 2;*/

            //CluwneLib.CurrentRenderTarget.FilledRectangle(x + lightVec.X, y + lightVec.Y, Sprite.Width + 1,Sprite.Height + 1, Color.FromArgb(0, Color.Transparent));
        }

        public override void DrawDecals(float xTopLeft, float yTopLeft, int tileSpacing, Batch decalBatch)
        {
            //d.Draw(xTopLeft, yTopLeft, tileSpacing, decalBatch);
        }

        public override void RenderTop(float xTopLeft, float yTopLeft, Batch wallTopsBatch)
        {
            int tileSpacing = mapMgr.GetTileSpacing();

            Vector2 SEpos = new Vector2();

            if (_dir == Direction.East)
            {
                topSpriteNW.SetPosition((float)bounds.X - xTopLeft - 12f,
                        (float)bounds.Y - (Sprite.Height - bounds.Height) - yTopLeft);

            //    SEpos += topSpriteNW.Position + new Vector2(tileSpacing, 0f);

            }
            else
            {
                topSpriteNW.SetPosition((float)bounds.X - xTopLeft,
                        (float)bounds.Y - (Sprite.Height - bounds.Height) - yTopLeft - 12f);

               // SEpos += topSpriteNW.Position + new Vector2(0f, tileSpacing);
            }

            topSpriteSE.SetPosition(SEpos.X, SEpos.Y);

            topSpriteNW.Color = Color.White;
            topSpriteSE.Color = Color.White;

            topSprite.Color = Color.White;
            topSprite.SetPosition((float)bounds.X - xTopLeft,
                       (float)bounds.Y - (Sprite.Height - bounds.Height) - yTopLeft);

            //wallTopsBatch.AddClone(topSprite);

            //wallTopsBatch.AddClone(topSpriteNW);
            //wallTopsBatch.AddClone(topSpriteSE);



        }
    }
}