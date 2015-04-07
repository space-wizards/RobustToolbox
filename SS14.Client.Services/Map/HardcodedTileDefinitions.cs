using GorgonLibrary.Graphics;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS14.Client.Services.Map
{
    [System.Diagnostics.DebuggerDisplay("TileDef: {Name}")]
    public sealed class SpaceTileDefinition : ITileDefinition
    {
        public static readonly ITileDefinition Instance = new SpaceTileDefinition();
        private SpaceTileDefinition() { }

        public ushort TileId { get { return 0; } }
        public void InvalidateTileId() { }

        public string Name { get { return "Space"; } }

        public bool IsConnectingSprite { get { return false; } }
        public bool IsOpaque { get { return false; } }
        public bool IsCollidable { get { return false; } }
        public bool IsGasVolume { get { return true; } }
        public bool IsVentedIntoSpace { get { return true; } }
        public bool IsWall { get { return false; } }

        public Tile Create(ushort data = 0) { return new Tile(0, data); }

        Sprite tileSprite;

        public void Render(float xTopLeft, float yTopLeft, GorgonLibrary.Graphics.Batch batch)
        {
            if (tileSprite == null)
                tileSprite = IoCManager.Resolve<IResourceManager>().GetSprite("space_texture");
            
            tileSprite.SetPosition(xTopLeft, yTopLeft);
            batch.AddClone(tileSprite);
        }

        public void RenderPos(float x, float y, int tileSpacing, int lightSize)
        {
        }

        public void RenderPosOffset(float x, float y, int tileSpacing, GorgonLibrary.Vector2D lightPosition)
        {
        }

        public void DrawDecals(float xTopLeft, float yTopLeft, int tileSpacing, GorgonLibrary.Graphics.Batch decalBatch)
        {
        }

        public void RenderGas(float xTopLeft, float yTopLeft, int tileSpacing, GorgonLibrary.Graphics.Batch gasBatch)
        {
        }

        public void RenderTop(float xTopLeft, float yTopLeft, GorgonLibrary.Graphics.Batch wallTopsBatch)
        {
        }
    }

    public sealed class FloorTileDefinition : TileDefinition
    {
        public FloorTileDefinition()
        {
            Name = "Floor";

            IsConnectingSprite = false;
            IsOpaque = false;
            IsCollidable = false;
            IsGasVolume = true;
            IsVentedIntoSpace = false;
            IsWall = false;

            tileSprite = IoCManager.Resolve<IResourceManager>().GetSprite("floor_texture");
        }
    }

    public sealed class WallTileDefinition : TileDefinition
    {
        public WallTileDefinition()
        {
            Name = "Wall";

            IsConnectingSprite = false;
            IsOpaque = true;
            IsCollidable = true;
            IsGasVolume = false;
            IsVentedIntoSpace = false;
            IsWall = true;

            tileSprite = IoCManager.Resolve<IResourceManager>().GetSprite("wall_texture");
        }
    }
}
