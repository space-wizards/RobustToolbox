using System.Collections.Generic;
using OpenTK;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Sprites;
using SS14.Shared.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Client.Interfaces.Player;
using SS14.Shared.Interfaces.GameObjects.Components;
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
            foreach (var tileReference in tileRefs)
            {
                var tileType = tileReference.TileDef;
                var point = CluwneLib.WorldToScreen(new Vector2(tileReference.X, tileReference.Y));
                RenderTile(tileType, point.X, point.Y, floorBatch);
                RenderGas(tileType, point.X, point.Y, tileReference.TileSize, gasBatch);
            }
        }

        /// <summary>
        ///     Render a textured instance of a tile at the given coordinates.
        /// </summary>
        /// <param name="def">The definition of the tile to use.</param>
        /// <param name="xTopLeft"></param>
        /// <param name="yTopLeft"></param>
        /// <param name="batch">The SpriteBatch to queue into.</param>
        public static void RenderTile(ITileDefinition def, float xTopLeft, float yTopLeft, SpriteBatch batch)
        {
            var tileSprite = IoCManager.Resolve<IResourceCache>().GetSprite(def.SpriteName);
            tileSprite.Position = new Vector2(xTopLeft, yTopLeft);
            batch.Draw(tileSprite);
        }

        /// <summary>
        ///     Renders a shadow cast from the object
        /// </summary>
        /// <param name="def">The definition of the tile to use.</param>
        /// <param name="x">X position in world coordinates.</param>
        /// <param name="y">Y position in world coordinates.</param>
        public static void RenderPos(IEntity entity, float x, float y)
        {
            var tileSprite = entity.GetComponent<ISpriteComponent>().GetCurrentSprite();
            var bounds = tileSprite.LocalBounds;
            var shape = new RectangleShape(new Vector2(bounds.Width, bounds.Height));
            shape.FillColor = Color.Red;
            shape.Position = new Vector2(x - bounds.Width / 2, y - bounds.Height / 2);
            shape.Draw(CluwneLib.CurrentRenderTarget, RenderStates.Default);
        }

        //What was this supposed to do?
        public static void RenderPosOffset(ITileDefinition def, float x, float y, int tileSpacing, Vector2 lightPosition)
        {
        }

        //What was this supposed to do?
        public static void DrawDecals(ITileDefinition def, float xTopLeft, float yTopLeft, int tileSpacing, SpriteBatch decalBatch)
        {
        }

        //What was this supposed to do?
        public static void RenderGas(ITileDefinition def, float xTopLeft, float yTopLeft, uint tileSpacing, SpriteBatch gasBatch)
        {
        }

        //What was this supposed to do?
        public static void RenderTop(ITileDefinition def, float xTopLeft, float yTopLeft, SpriteBatch wallTopsBatch)
        {
        }
    }
}
