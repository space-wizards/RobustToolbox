using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13_Shared;

namespace ClientInterfaces.Map
{
    public interface ITile
    {
        Vector2D Position { get; }
        Point TilePosition { get; }
        bool Visible { get; set; }
        bool ConnectSprite { get; set; }
        bool Opaque { get; set; }
        void Render(float xTopLeft, float yTopLeft, int tileSpacing, Batch batch);
        void RenderPos(float x, float y, int tileSpacing, int lightSize);
        void RenderPosOffset(float x, float y, int tileSpacing, Vector2D lightPosition);
        void DrawDecals(float xTopLeft, float yTopLeft, int tileSpacing, Batch decalBatch);
        void RenderGas(float xTopLeft, float yTopLeft, int tileSpacing, Batch gasBatch);
        void RenderTop(float xTopLeft, float yTopLeft, int tileSpacing, Batch wallTopsBatch);
    }
}
