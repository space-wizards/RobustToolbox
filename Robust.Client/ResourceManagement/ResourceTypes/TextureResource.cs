using System.IO;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Interfaces.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.ResourceManagement
{
    public class TextureResource : BaseResource
    {
        public const float ClickThreshold = 0.25f;

        public override ResourcePath? Fallback => new("/Textures/noSprite.png");
        public Texture Texture { get; private set; } = default!;

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            if (!cache.TryContentFileRead(path, out var stream))
            {
                throw new FileNotFoundException("Content file does not exist for texture");
            }

            using (stream)
            {
                // Primarily for tracking down iCCP sRGB errors in the image files.
                Logger.DebugS("res.tex", $"Loading texture {path}.");

                var loadParameters = _tryLoadTextureParameters(cache, path) ?? TextureLoadParameters.Default;

                var manager = IoCManager.Resolve<IClyde>();

                using var image = Image.Load<Rgba32>(stream);

                Texture = manager.LoadTextureFromImage(image, path.ToString(), loadParameters);

                if (cache is IResourceCacheInternal cacheInternal)
                {
                    cacheInternal.TextureLoaded(new TextureLoadedEventArgs(path, image, this));
                }
            }
        }

        private static TextureLoadParameters? _tryLoadTextureParameters(IResourceCache cache, ResourcePath path)
        {
            var metaPath = path.WithName(path.Filename + ".yml");
            if (cache.TryContentFileRead(metaPath, out var stream))
            {
                using (stream)
                {
                    YamlDocument yamlData;
                    using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
                    {
                        var yamlStream = new YamlStream();
                        yamlStream.Load(reader);
                        if (yamlStream.Documents.Count == 0)
                        {
                            return null;
                        }

                        yamlData = yamlStream.Documents[0];
                    }

                    return TextureLoadParameters.FromYaml((YamlMappingNode) yamlData.RootNode);
                }
            }

            return null;
        }

        // TODO: Due to a bug in Roslyn, NotNullIfNotNullAttribute doesn't work.
        // So this can't work with both nullables and non-nullables at the same time.
        // I decided to only have it work with non-nullables as such.
        public static implicit operator Texture(TextureResource res)
        {
            return res.Texture;
        }
    }
}
