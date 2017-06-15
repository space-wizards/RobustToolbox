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
    public static class MapRenderer
    {
        private static Dictionary<ushort, Sprite> _spriteLookup = new Dictionary<ushort, Sprite>();

        public static bool HasSprite(ITileDefinition tileDef)
        {
            if (_spriteLookup == null)
                return false;

            return _spriteLookup.ContainsKey(tileDef.TileId);
        }

        public static Sprite GetSprite(ITileDefinition tileDef)
        {
            Sprite output;
            if (_spriteLookup != null && _spriteLookup.TryGetValue(tileDef.TileId, out output))
                return output;

            return null;
        }

        public static void SetSprite(ITileDefinition tileDef, Sprite sprite)
        {
            _spriteLookup.Add(tileDef.TileId, sprite);
        }

        public static void RebuildSprites(ITileDefinitionManager defManager)
        {
            foreach (ITileDefinition def in defManager)
            {
                var sprite = IoCManager.Resolve<IResourceManager>().GetSprite(def.SpriteName);
                _spriteLookup.Add(def.TileId, sprite);
            }
        }

        public static void DrawTiles(IEnumerable<TileRef> tileRefs, SpriteBatch floorBatch, SpriteBatch gasBatch, SpriteBatch wallBatch, SpriteBatch wallTopsBatch)
        {
            var walls = new List<TileRef>();

            foreach (var tileReference in tileRefs)
            {
                var tile = tileReference.Tile;
                var tileType = tile.TileDef;
                
                if (tileType.IsWall)
                    walls.Add(tileReference);
                else
                {
                    var point = CluwneLib.WorldToScreen(new Vector2f(tileReference.X, tileReference.Y));
                    RenderTile(tileType, point.X, point.Y, floorBatch);
                    RenderGas(tileType, point.X, point.Y, tileReference.TileSize, gasBatch);
                }

            }

            walls.Sort((t1, t2) => t1.Y - t2.Y);

            foreach (TileRef tr in walls)
            {
                var t = tr.Tile;
                var td = t.TileDef;

                var point = CluwneLib.WorldToScreen(new Vector2f(tr.X, tr.Y));
                RenderTile(td, point.X, point.Y, wallBatch);
                RenderTop(td, point.X, point.Y, wallTopsBatch);
            }
        }

        public static void RenderTile(ITileDefinition def, float xTopLeft, float yTopLeft, SpriteBatch batch)
        {
            Sprite tileSprite;
            if (_spriteLookup.TryGetValue(def.TileId, out tileSprite))
            {
                tileSprite.Position = new Vector2f(xTopLeft, yTopLeft);
                batch.Draw(tileSprite);
            }
        }

        public static void RenderPos(ITileDefinition def, float x, float y)
        {
        }

        public static void RenderPosOffset(ITileDefinition def, float x, float y, int tileSpacing, Vector2f lightPosition)
        {
        }

        public static void DrawDecals(ITileDefinition def, float xTopLeft, float yTopLeft, int tileSpacing, SpriteBatch decalBatch)
        {
        }

        public static void RenderGas(ITileDefinition def, float xTopLeft, float yTopLeft, int tileSpacing, SpriteBatch gasBatch)
        {
        }

        public static void RenderTop(ITileDefinition def, float xTopLeft, float yTopLeft, SpriteBatch wallTopsBatch)
        {
        }
    }
}
