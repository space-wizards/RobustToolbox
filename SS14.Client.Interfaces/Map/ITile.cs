using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Shared;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Map
{
    public interface ITile
    {
        Vector2 Position { get; }
        bool Visible { get; set; }
        bool ConnectSprite { get; set; }
        bool Opaque { get; set; }
        bool IsSolidTile();
        void Render(float xTopLeft, float yTopLeft, SpriteBatch batch);
        void RenderPos(float x, float y, int tileSpacing, int lightSize);
        void RenderPosOffset(float x, float y, int tileSpacing, Vector2 lightPosition);
        void DrawDecals(float xTopLeft, float yTopLeft, int tileSpacing, SpriteBatch decalBatch);
        void RenderGas(float xTopLeft, float yTopLeft, int tileSpacing, SpriteBatch gasBatch);
        void RenderTop(float xTopLeft, float yTopLeft, SpriteBatch wallTopsBatch);
    }
}