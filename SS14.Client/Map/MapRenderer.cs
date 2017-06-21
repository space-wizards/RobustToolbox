using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace SS14.Client.Map
{
    /// <summary>
    ///     Helper functions for rendering a map.
    ///     TODO: This is a temporary class. When the rendering engine is remade, it should provide equivalent functionality.
    /// </summary>
    public static class MapRenderer
    {
        public static void DrawTiles(IEnumerable<TileRef> tileRefs, SpriteBatch floorBatch, SpriteBatch gasBatch, SpriteBatch wallBatch, SpriteBatch wallTopsBatch)
        {
            var walls = new List<TileRef>();

            foreach (var tileReference in tileRefs)
            {
                var tileType = tileReference.TileDef;

                if (tileType.IsWall)
                {
                    walls.Add(tileReference);
                }
                else
                {
                    var point = CluwneLib.WorldToScreen(new Vector2f(tileReference.X, tileReference.Y));
                    RenderTile(tileType, point.X, point.Y, floorBatch);
                    RenderGas(tileType, point.X, point.Y, tileReference.TileSize, gasBatch);
                }
            }

            walls.Sort((t1, t2) => (int) t1.Y - (int) t2.Y);

            foreach (var tr in walls)
            {
                var td = tr.TileDef;

                var point = CluwneLib.WorldToScreen(new Vector2f(tr.X, tr.Y));
                RenderTile(td, point.X, point.Y, wallBatch);
                RenderTop(td, point.X, point.Y, wallTopsBatch);
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
            var tileSprite = IoCManager.Resolve<IResourceManager>().GetSprite(def.SpriteName);
            tileSprite.Position = new Vector2f(xTopLeft, yTopLeft);
            batch.Draw(tileSprite);
        }

        /// <summary>
        ///     Render a solid black instance of a tile at the given coordinates.
        /// </summary>
        /// <param name="def">The definition of the tile to use.</param>
        /// <param name="x">X position in world coordinates.</param>
        /// <param name="y">Y position in world coordinates.</param>
        public static void RenderPos(ITileDefinition def, float x, float y)
        {
            var tileSprite = IoCManager.Resolve<IResourceManager>().GetSprite(def.SpriteName);
            var bounds = tileSprite.GetLocalBounds();
            var shape = new RectangleShape(new Vector2f(bounds.Width, bounds.Height));
            shape.FillColor = Color.Red;
            shape.Position = new Vector2f(x, y);
            shape.Draw(CluwneLib.CurrentRenderTarget, RenderStates.Default);
        }

        //What was this supposed to do?
        public static void RenderPosOffset(ITileDefinition def, float x, float y, int tileSpacing, Vector2f lightPosition)
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
