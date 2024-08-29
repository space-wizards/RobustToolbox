using System.IO;
using System.Threading;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.ResourceManagement
{
    public sealed class TextureResource : BaseResource
    {
        private OwnedTexture _texture = default!;
        public override ResPath? Fallback => new("/Textures/noSprite.png");

        public Texture Texture => _texture;

        public override void Load(IDependencyCollection dependencies, ResPath path)
        {
            if (path.Directory.Filename.EndsWith(".rsi"))
            {
                Logger.WarningS(
                    "res",
                    "Loading raw texture inside RSI: {Path}. Refer to the RSI state instead of the raw PNG.",
                    path);
            }

            var data = new LoadStepData {Path = path};

            LoadPreTexture(dependencies.Resolve<IResourceManager>(), data);
            LoadTexture(dependencies.Resolve<IClyde>(), data);
            LoadFinish(dependencies.Resolve<IResourceCache>(), data);
        }

        internal static void LoadPreTexture(IResourceManager cache, LoadStepData data)
        {
            using (var stream = cache.ContentFileRead(data.Path))
            {
                data.Image = Image.Load<Rgba32>(stream);
            }

            data.LoadParameters = TryLoadTextureParameters(cache, data.Path) ?? TextureLoadParameters.Default;
        }

        internal static void LoadTexture(IClyde clyde, LoadStepData data)
        {
            data.Texture = clyde.LoadTextureFromImage(data.Image, data.Path.ToString(), data.LoadParameters);
        }

        internal void LoadFinish(IResourceCache cache, LoadStepData data)
        {
            _texture = data.Texture;

            if (cache is IResourceCacheInternal cacheInternal)
            {
                cacheInternal.TextureLoaded(new TextureLoadedEventArgs(data.Path, data.Image, this));
            }

            data.Image.Dispose();
        }

        private static TextureLoadParameters? TryLoadTextureParameters(IResourceManager cache, ResPath path)
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

        public override void Reload(IDependencyCollection dependencies, ResPath path, CancellationToken ct = default)
        {
            var data = new LoadStepData {Path = path};

            LoadPreTexture(dependencies.Resolve<IResourceManager>(), data);

            if (data.Image.Width == Texture.Width && data.Image.Height == Texture.Height)
            {
                // Dimensions match, rewrite texture in place.
                _texture.SetSubImage(Vector2i.Zero, data.Image);
            }
            else
            {
                // Dimensions do not match, make new texture.
                _texture.Dispose();
                LoadTexture(dependencies.Resolve<IClyde>(), data);
                _texture = data.Texture;
            }

            data.Image.Dispose();
        }

        internal sealed class LoadStepData
        {
            public ResPath Path = default!;
            public Image<Rgba32> Image = default!;
            public TextureLoadParameters LoadParameters;
            public OwnedTexture Texture = default!;
            public bool Bad;
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
