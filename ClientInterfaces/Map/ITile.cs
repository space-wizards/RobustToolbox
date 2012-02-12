using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13_Shared;

namespace ClientInterfaces.Map
{
    public interface ITile
    {
        TileType TileType { get; }
        Vector2D Position { get; }
        Point TilePosition { get; }
        bool Visible { get; set; }

        void Render(float xTopLeft, float yTopLeft, int tileSpacing);
        void RenderLight(float xTopLeft, float yTopLeft, int tileSpacing, Batch lightMapBatch);
        void DrawDecals(float xTopLeft, float yTopLeft, int tileSpacing, Batch decalBatch);
        void RenderGas(float xTopLeft, float yTopLeft, int tileSpacing, Batch gasBatch);
        void RenderTop(float xTopLeft, float yTopLeft, int tileSpacing, Batch wallTopsBatch);
    }
}
