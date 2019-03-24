using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SS14.Client.Graphics;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.GameObjects.Components.Renderable;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using Image = SixLabors.ImageSharp.Image;

namespace SS14.Client.Map
{
    internal sealed class ClydeTileDefinitionManager : TileDefinitionManager, IClydeTileDefinitionManager
    {
        [Dependency] private IResourceCache _resourceCache;

        public Texture TileTextureAtlas { get; private set; }

        private readonly Dictionary<ushort, UIBox2> _tileRegions = new Dictionary<ushort, UIBox2>();

        public UIBox2? TileAtlasRegion(Tile tile)
        {
            if (_tileRegions.TryGetValue(tile.TileId, out var region))
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
            var defList = TileDefs.Where(t => !string.IsNullOrEmpty(t.SpriteName)).ToList();
            const int tileSize = EyeManager.PIXELSPERMETER;

            var dimensionX = (int) Math.Ceiling(Math.Sqrt(defList.Count));
            var dimensionY = (int) Math.Ceiling((float) defList.Count / dimensionX);

            var sheet = new Image<Rgba32>(dimensionX * tileSize, dimensionY * tileSize);

            for (var i = 0; i < defList.Count; i++)
            {
                var def = defList[i];
                var column = i % dimensionX;
                var row = i / dimensionX;

                Image<Rgba32> image;
                using (var stream = _resourceCache.ContentFileRead($"/Textures/Tiles/{def.SpriteName}.png"))
                {
                    image = Image.Load(stream);
                }

                if (image.Width != tileSize || image.Height != tileSize)
                {
                    throw new NotImplementedException("Unable to use tiles with a dimension other than 32x32.");
                }

                var point = new Point(column * tileSize, row * tileSize);

                sheet.Mutate(x => x.DrawImage(image, point,
                    PixelColorBlendingMode.Overlay, 1));

                _tileRegions.Add(def.TileId,
                    UIBox2.FromDimensions(
                        point.X / (float) sheet.Width, point.Y / (float) sheet.Height,
                        tileSize / (float) sheet.Width, tileSize / (float) sheet.Height));
            }

            TileTextureAtlas = Texture.LoadFromImage(sheet, "Tile Atlas");
        }
    }
}
