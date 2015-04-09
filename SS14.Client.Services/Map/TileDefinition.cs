using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Client.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System.Diagnostics;

namespace SS14.Client.Services.Map
{
    [System.Diagnostics.DebuggerDisplay("TileDef: {Name}")]
    public class TileDefinition : ITileDefinition
    {
        protected CluwneSprite tileSprite { get; set; }

        ushort tileId = ushort.MaxValue;
        public ushort TileId
        {
            get {
                if (tileId == ushort.MaxValue)
                    tileId = IoCManager.Resolve<ITileDefinitionManager>().Register(this);

                Debug.Assert(tileId != ushort.MaxValue);
                return tileId;
            }
        }
        public void InvalidateTileId()
        {
            tileId = ushort.MaxValue;
        }

        public string Name { get; protected set; }

        public bool IsConnectingSprite { get; protected set; }
        public bool IsOpaque { get; protected set; }
        public bool IsCollidable { get; protected set; }
        public bool IsGasVolume { get; protected set; }
        public bool IsVentedIntoSpace { get; protected set; }
        public bool IsWall { get; protected set; }

        public Tile Create(ushort data = 0) { return new Tile(TileId, data); }


        public void Render(float xTopLeft, float yTopLeft, SpriteBatch batch)
        {
            if (tileSprite != null)
            {
                tileSprite.SetPosition(xTopLeft, yTopLeft);
                batch.Draw(tileSprite);
            }
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
}
