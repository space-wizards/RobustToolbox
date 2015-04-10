using SS14.Client.Graphics.Sprite;
using SS14.Shared;
using SS14.Shared.Maths;

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

        void Render(float xTopLeft, float yTopLeft, SpriteBatch batch);
        void RenderPos(float x, float y, int tileSpacing, int lightSize);
        void RenderPosOffset(float x, float y, int tileSpacing, Vector2 lightPosition);
        void DrawDecals(float xTopLeft, float yTopLeft, int tileSpacing, SpriteBatch decalBatch);
        void RenderGas(float xTopLeft, float yTopLeft, int tileSpacing, SpriteBatch gasBatch);
        void RenderTop(float xTopLeft, float yTopLeft, SpriteBatch wallTopsBatch);
    }
}