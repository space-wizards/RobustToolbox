using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;

namespace SS14.Client.Map
{
    [System.Diagnostics.DebuggerDisplay("TileDef: {Name}")]
    public sealed class SpaceTileDefinition : ITileDefinition
    {
        public void InitializeResources(IResourceManager resourceManager)
        {
            tileSprite = resourceManager.GetSprite("space_texture");
        }

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

        public void Render(float xTopLeft, float yTopLeft, SpriteBatch batch)
        {
            tileSprite.Position = new SFML.System.Vector2f(xTopLeft, yTopLeft);
            batch.Draw(tileSprite);
        }

        public void RenderPos(float x, float y)
        {

        }

        public void RenderPosOffset(float x, float y, int tileSpacing, Vector2f lightPosition)
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
        }

        public override void InitializeResources(IResourceManager resourceManager)
        {
            tileSprite = resourceManager.GetSprite("floor_texture");
        }
    }

    public sealed class WallTileDefinition : TileDefinition
    {
        private RectangleShape shape;

        public WallTileDefinition()
        {
            Name = "Wall";

            IsConnectingSprite = false;
            IsOpaque = true;
            IsCollidable = true;
            IsGasVolume = false;
            IsVentedIntoSpace = false;
            IsWall = true;
        }
        
        public override void RenderPos(float x, float y)
        {
            shape.FillColor = Color.Black;
            shape.Position = new SFML.System.Vector2f(x, y);
            shape.Draw(CluwneLib.CurrentRenderTarget, RenderStates.Default);
        }

        public override void InitializeResources(IResourceManager resourceManager)
        {
            tileSprite = resourceManager.GetSprite("wall_texture");
            var bounds = tileSprite.GetLocalBounds();
            shape = new RectangleShape(new SFML.System.Vector2f(bounds.Width, bounds.Height));
        }
    }
}
