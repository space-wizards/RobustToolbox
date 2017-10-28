using OpenTK;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using System.Collections.Generic;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Map
{
    /// <summary>
    ///     Helper functions for rendering a map.
    ///     TODO: This is a temporary class. When the rendering engine is remade, it should provide equivalent functionality.
    /// </summary>
    public static class MapRenderer
    {
        public static void DrawTiles(IEnumerable<TileRef> tileRefs, SpriteBatch floorBatch, SpriteBatch gasBatch)
        {
            var cache = IoCManager.Resolve<IResourceCache>();
            Sprite sprite = null;
            ITileDefinition lastDef = null;
            var ppm = CluwneLib.Camera.PixelsPerMeter;
            foreach (var tileReference in tileRefs)
            {
                if (tileReference.TileDef != lastDef)
                {
                    lastDef = tileReference.TileDef;
                    sprite = cache.GetSprite(lastDef.SpriteName);
                }
                sprite.Position = new Vector2(tileReference.X, tileReference.Y) * ppm;
                floorBatch.Draw(sprite);
            }
        }

        /// <summary>
        ///     Renders a shadow cast from the object
        /// </summary>
        /// <param name="occluder">The occluder component to use.</param>
        /// <param name="x">X position in world coordinates.</param>
        /// <param name="y">Y position in world coordinates.</param>
        public static void RenderPos(Box2 bounds, float x, float y)
        {
            var shape = new RectangleShape(new Vector2(bounds.Width, bounds.Height))
            {
                FillColor = Color.Red,
                Position = new Vector2(x - bounds.Width / 2 + bounds.Left, y - bounds.Height / 2 + bounds.Top)
            };
            shape.Draw(CluwneLib.CurrentRenderTarget, RenderStates.Default);
        }

        //What was this supposed to do?
        public static void RenderPosOffset(ITileDefinition def, float x, float y, int tileSpacing, Vector2 lightPosition)
        {
            //TODO: Figure out what to do with this
        }

        //What was this supposed to do?
        public static void DrawDecals(ITileDefinition def, float xTopLeft, float yTopLeft, int tileSpacing, SpriteBatch decalBatch)
        {
            //TODO: Figure out what to do with this
        }

        //What was this supposed to do?
        public static void RenderGas(ITileDefinition def, float xTopLeft, float yTopLeft, uint tileSpacing, SpriteBatch gasBatch)
        {
            //TODO: Figure out what to do with this
        }

        //What was this supposed to do?
        public static void RenderTop(ITileDefinition def, float xTopLeft, float yTopLeft, SpriteBatch wallTopsBatch)
        {
            //TODO: Figure out what to do with this
        }
    }
}
