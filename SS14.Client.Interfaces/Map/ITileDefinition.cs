using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace SS14.Client.Interfaces.Map
{
    public interface ITileDefinition
    {
        ushort TileId { get; }
        void InvalidateTileId();

        string Name { get; }
        bool IsConnectingSprite { get; }
        bool IsOpaque { get; }
        bool IsCollidable { get; }
        bool IsGasVolume { get; }
        bool IsVentedIntoSpace { get; }
        //bool IsFloor { get; } // TODO: Determine if we want this.
        bool IsWall { get; }

        Tile Create(ushort data = 0);

        void Render(float xTopLeft, float yTopLeft, Batch batch);
        void RenderPos(float x, float y, int tileSpacing, int lightSize);
        void RenderPosOffset(float x, float y, int tileSpacing, Vector2D lightPosition);
        void DrawDecals(float xTopLeft, float yTopLeft, int tileSpacing, Batch decalBatch);
        void RenderGas(float xTopLeft, float yTopLeft, int tileSpacing, Batch gasBatch);
        void RenderTop(float xTopLeft, float yTopLeft, Batch wallTopsBatch);
    }
}