using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Robust.Client.Map
{
    internal sealed class ClydeTileDefinitionManager : TileDefinitionManager, IClydeTileDefinitionManager
    {
        [Dependency] private readonly IResourceManager _manager = default!;

        private Texture? _tileTextureAtlas;

        public Texture TileTextureAtlas => _tileTextureAtlas ?? Texture.Transparent;

        private FrozenDictionary<(int Id, Direction Direction), Box2[]> _tileRegions = FrozenDictionary<(int Id, Direction Direction), Box2[]>.Empty;

        public Box2 ErrorTileRegion { get; private set; }

        /// <inheritdoc />
        public Box2[]? TileAtlasRegion(Tile tile)
        {
            return TileAtlasRegion(tile.TypeId);
        }

        /// <inheritdoc />
        public Box2[]? TileAtlasRegion(int tileType)
        {
            return TileAtlasRegion(tileType, Direction.Invalid);
        }

        /// <inheritdoc />
        public Box2[]? TileAtlasRegion(int tileType, Direction direction)
        {
            // ReSharper disable once CanSimplifyDictionaryTryGetValueWithGetValueOrDefault
            if (_tileRegions.TryGetValue((tileType, direction), out var region))
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

        internal void _genTextureAtlas()
        {
            var tileRegs = new Dictionary<(int Id, Direction Direction), Box2[]>();
            _tileTextureAtlas = null;

            var defList = TileDefs.Where(t => t.Sprite != null).ToList();

            // If there are no tile definitions, we do nothing.
            if (defList.Count <= 0)
                return;

            const int tileSize = EyeManager.PixelsPerMeter;

            var tileCount = defList.Select(x => x.Variants + x.EdgeSprites.Count).Sum() + 1;

            var dimensionX = (int) Math.Ceiling(Math.Sqrt(tileCount));
            var dimensionY = (int) Math.Ceiling((float) tileCount / dimensionX);

            var imgWidth = dimensionX * tileSize;
            var imgHeight = dimensionY * tileSize;
            var sheet = new Image<Rgba32>(imgWidth, imgHeight);
            var w = (float) sheet.Width;
            var h = (float) sheet.Height;

            // Add in the missing tile texture sprite as tile texture 0.
            {
                ErrorTileRegion = Box2.FromDimensions(
                    0, (h - EyeManager.PixelsPerMeter) / h,
                    tileSize / w, tileSize / h);
                Image<Rgba32> image;
                using (var stream = _manager.ContentFileRead("/Textures/noTile.png"))
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
                // Already know it's not null above
                var path = def.Sprite!.Value;

                using (var stream = _manager.ContentFileRead(path))
                {
                    image = Image.Load<Rgba32>(stream);
                }

                if (image.Width != (tileSize * def.Variants) || image.Height != tileSize)
                {
                    throw new NotSupportedException(
                        $"Unable to load {path}, due to being unable to use tile texture with a dimension other than {tileSize}x({tileSize} * Variants).");
                }

                var regionList = new Box2[def.Variants];

                for (var j = 0; j < def.Variants; j++)
                {
                    var point = new Vector2i(column * tileSize, row * tileSize);

                    var box = new UIBox2i(0, 0, tileSize, tileSize).Translated(new Vector2i(j * tileSize, 0));
                    image.Blit(box, sheet, point);

                    regionList[j] = Box2.FromDimensions(
                        point.X / w, (h - point.Y - EyeManager.PixelsPerMeter) / h,
                        tileSize / w, tileSize / h);
                    BumpColumn(ref row, ref column, dimensionX);
                }

                tileRegs.Add((def.TileId, Direction.Invalid), regionList);

                // Edges
                if (def.EdgeSprites.Count > 0)
                {
                    for (var x = -1; x <= 1; x++)
                    {
                        for (var y = -1; y <= 1; y++)
                        {
                            if (x == 0 && y == 0)
                                continue;

                            var direction = new Vector2i(x, y).AsDirection();
                            if (!def.EdgeSprites.TryGetValue(direction, out var edge))
                                continue;

                            using (var stream = _manager.ContentFileRead(edge))
                            {
                                image = Image.Load<Rgba32>(stream);
                            }

                            if (image.Width != tileSize || image.Height != tileSize)
                            {
                                throw new NotSupportedException(
                                    $"Unable to load {path}, due to being unable to use tile textures with a dimension other than {tileSize}x{tileSize}.");
                            }

                            Angle angle = Angle.Zero;

                            switch (direction)
                            {
                                // Corner sprites
                                case Direction.SouthEast:
                                    break;
                                case Direction.NorthEast:
                                    angle = new Angle(MathF.PI / 2f);
                                    break;
                                case Direction.NorthWest:
                                    angle = new Angle(MathF.PI);
                                    break;
                                case Direction.SouthWest:
                                    angle = new Angle(MathF.PI * 1.5f);
                                    break;
                                // Edge sprites
                                case Direction.South:
                                    break;
                                case Direction.East:
                                    angle = new Angle(MathF.PI / 2f);
                                    break;
                                case Direction.North:
                                    angle = new Angle(MathF.PI);
                                    break;
                                case Direction.West:
                                    angle = new Angle(MathF.PI * 1.5f);
                                    break;
                            }

                            if (angle != Angle.Zero)
                            {
                                image.Mutate(o => o.Rotate((float)-angle.Degrees));
                            }

                            var point = new Vector2i(column * tileSize, row * tileSize);
                            var box = new UIBox2i(0, 0, tileSize, tileSize);
                            image.Blit(box, sheet, point);

                            // If you ever need edge variants then you could just bump this.
                            var edgeList = new Box2[def.EdgeSprites.Count];
                            edgeList[0] = Box2.FromDimensions(
                                point.X / w, (h - point.Y - EyeManager.PixelsPerMeter) / h,
                                tileSize / w, tileSize / h);

                            tileRegs.Add((def.TileId, direction), edgeList);
                            BumpColumn(ref row, ref column, dimensionX);
                        }
                    }
                }
            }

            _tileRegions = tileRegs.ToFrozenDictionary();
            _tileTextureAtlas = Texture.LoadFromImage(sheet, "Tile Atlas");
        }

        private void BumpColumn(ref int row, ref int column, int dimensionX)
        {
            column++;

            if (column >= dimensionX)
            {
                column = 0;
                row++;
            }
        }
    }

    public sealed class ReloadTileTexturesCommand : LocalizedCommands
    {
        [Dependency] private readonly ClydeTileDefinitionManager _tile = default!;

        public override string Command => "reloadtiletextures";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _tile._genTextureAtlas();
        }
    }
}

