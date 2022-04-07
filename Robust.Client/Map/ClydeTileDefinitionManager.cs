using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Map
{
    internal sealed class ClydeTileDefinitionManager : TileDefinitionManager, IClydeTileDefinitionManager
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private Texture? _tileTextureAtlas;

        public Texture TileTextureAtlas => _tileTextureAtlas ?? Texture.Transparent;

        private readonly Dictionary<ushort, Box2[]> _tileRegions = new();

        public Box2 ErrorTileRegion { get; private set; }

        /// <inheritdoc />
        public Box2[]? TileAtlasRegion(Tile tile)
        {
            return TileAtlasRegion(tile.TypeId);
        }

        /// <inheritdoc />
        public Box2[]? TileAtlasRegion(ushort tileType)
        {
            if (_tileRegions.TryGetValue(tileType, out var region))
            {
                return region;
            }

            return null;
        }

        public override void Initialize()
        {
            base.Initialize();

            _genTextureAtlas();
        }

        private void _genTextureAtlas()
        {
            var tileCount = 1;
            // - 1 for space.
            var defList = new List<ITileDefinition>(TileDefs.Count - 1);

            foreach (var def in TileDefs)
            {
                if (string.IsNullOrEmpty(def.SpriteName)) continue;
                defList.Add(def);
                tileCount += GetTileCount(def);
            }

            // If there are no tile definitions, we do nothing.
            if (defList.Count <= 0)
                return;

            const int tileSize = EyeManager.PixelsPerMeter;

            var dimensionX = (int) Math.Ceiling(Math.Sqrt(tileCount));
            var dimensionY = (int) Math.Ceiling((float) tileCount / dimensionX);

            var imgWidth = dimensionX * tileSize;
            var imgHeight = dimensionY * tileSize;
            var sheet = new Image<Rgba32>(imgWidth, imgHeight);

            // Add in the missing tile texture sprite as tile texture 0.
            {
                var w = (float) sheet.Width;
                var h = (float) sheet.Height;
                ErrorTileRegion = Box2.FromDimensions(
                    0, (h - EyeManager.PixelsPerMeter) / h,
                    tileSize / w, tileSize / h);
                Image<Rgba32> image;
                using (var stream = _resourceCache.ContentFileRead("/Textures/noTile.png"))
                {
                    image = Image.Load<Rgba32>(stream);
                }

                image.Blit(new UIBox2i(0, 0, tileSize, tileSize), sheet, Vector2i.Zero);
            }

            if (imgWidth >= 2048 || imgHeight >= 2048)
            {
                // Sanity warning, some machines don't have textures larger than this and need multiple atlases.
                Logger.WarningS("clyde",
                    $"Tile texture atlas is ({imgWidth} x {imgHeight}), larger than 2048 x 2048. If you really need {tileCount} tiles, file an issue on RobustToolbox.");
            }

            var column = 1;
            var row = 0;
            foreach (var def in defList)
            {
                Image<Rgba32> image;
                using (var stream = _resourceCache.ContentFileRead(new ResourcePath(def.Path) / $"{def.SpriteName}.png"))
                {
                    image = Image.Load<Rgba32>(stream);
                }

                if (image.Width != (tileSize * def.Variants) || image.Height != tileSize * ((def.Flags & TileDefFlag.Diagonals) != 0x0 ? 5 : 1))
                {
                    throw new NotSupportedException(
                        $"Unable to load {new ResourcePath(def.Path) / $"{def.SpriteName}.png"}, due to being unable to use tile texture with a dimension other than {tileSize} * Variants x {tileSize} x (full + diagonals).");
                }

                var regionList = new Box2[def.Variants];

                for (var j = 0; j < def.Variants; j++)
                {
                    // Lord this is uggo
                    regionList[j] = Update(sheet, image, j, column, row, tileSize, 0);
                    column++;
                    if (column >= dimensionX)
                    {
                        column = 0;
                        row++;
                    }

                    if ((def.Flags & TileDefFlag.Diagonals) != 0x0)
                    {
                        for (var i = 1; i < 5; i++)
                        {
                            regionList[j] = Update(sheet, image, j, column, row, tileSize, i);
                            if (column >= dimensionX)
                            {
                                column = 0;
                                row++;
                            }
                        }
                    }
                }

                _tileRegions.Add(def.TileId, regionList);
            }

            _tileTextureAtlas = Texture.LoadFromImage(sheet, "Tile Atlas");
        }

        private int GetTileCount(ITileDefinition def)
        {
            return (def.Flags & TileDefFlag.Diagonals) != 0x0 ? def.Variants * 4 : def.Variants;
        }

        private Box2 Update(Image<Rgba32> sheet, Image<Rgba32> image, int index, int column, int row, int tileSize, int yIndex)
        {
            var point = new Vector2i(column * tileSize, row * tileSize);
            var box = new UIBox2i(0, 0, tileSize, tileSize).Translated(new Vector2i(index * tileSize, yIndex));
            image.Blit(box, sheet, point);

            var w = (float) sheet.Width;
            var h = (float) sheet.Height;
            return Box2.FromDimensions(
                point.X / w, (h - point.Y - EyeManager.PixelsPerMeter) / h,
                tileSize / w, tileSize / h);
        }
    }
}
