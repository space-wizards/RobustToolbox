using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

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

        CluwneSprite tileSprite;

        public void Render(float xTopLeft, float yTopLeft, SpriteBatch batch)
        {
            if (tileSprite == null)
                tileSprite = IoCManager.Resolve<IResourceManager>().GetSprite("space_texture");
            
            tileSprite.SetPosition(xTopLeft, yTopLeft);
            batch.Draw(tileSprite);
        }

        public void RenderPos(float x, float y, int tileSpacing, int lightSize)
        {
        }

        public void RenderPosOffset(float x, float y, int tileSpacing, Vector2 lightPosition)
        {
        }

        public void DrawDecals(float xTopLeft, float yTopLeft, int tileSpacing, SpriteBatch decalBatch)
        {
        }

        public void RenderGas(float xTopLeft, float yTopLeft, int tileSpacing, SpriteBatch gasBatch)
        {
        }

        public void RenderTop(float xTopLeft, float yTopLeft, SpriteBatch wallTopsBatch)
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
