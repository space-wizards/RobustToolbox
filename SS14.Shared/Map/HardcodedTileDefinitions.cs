using SFML.Graphics;
using SFML.System;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;

namespace SS14.Shared.Map
{
    [System.Diagnostics.DebuggerDisplay("TileDef: {Name}")]
    public sealed class SpaceTileDefinition : ITileDefinition
    {
        public void InitializeResources(IResourceManager resourceManager)
        {
            tileSprite = resourceManager.GetSprite("space_texture");
        }

        public ushort TileId => 0;
        public void InvalidateTileId() { }

        public string Name => "Space";

        public bool IsConnectingSprite => false;
        public bool IsOpaque => false;
        public bool IsCollidable => false;
        public bool IsGasVolume => true;
        public bool IsVentedIntoSpace => true;
        public bool IsWall => false;
        public string SpriteName => "space_texture";

        public Tile Create(ushort data = 0) { return new Tile(0, data); }

        //Sprite tileSprite;
/*
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
*/
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
            SpriteName = "floor_texture";
        }

            tileSprite = IoCManager.Resolve<IResourceManager>().GetSprite("floor_texture");
        }
    }

    public sealed class WallTileDefinition : TileDefinition
    {
        private readonly RectangleShape shape;

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
        /*
        public override void RenderPos(float x, float y)
        {
            shape.FillColor = Color.Black;
            shape.Position = new SFML.System.Vector2f(x, y);
            shape.Draw(CluwneLib.CurrentRenderTarget, RenderStates.Default);
        }
        */
    }
}
