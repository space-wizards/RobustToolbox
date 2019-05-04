using System;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using System.IO;
using System.Text;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.IoC;
using SixLabors.ImageSharp;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.ResourceManagement
{
    public class TextureResource : BaseResource
    {
        public override ResourcePath Fallback { get; } = new ResourcePath("/Textures/noSprite.png");
        public Texture Texture { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            if (!cache.ContentFileExists(path))
            {
                throw new FileNotFoundException("Content file does not exist for texture");
            }

            // Primarily for tracking down iCCP sRGB errors in the image files.
            Logger.DebugS("res.tex", $"Loading texture {path}.");

            var loadParameters = _tryLoadTextureParameters(cache, path) ?? TextureLoadParameters.Default;

            switch (GameController.Mode)
            {
                case GameController.DisplayMode.Headless:
                    var image = Image.Load(cache.ContentFileRead(path).ToArray());
                    Texture = new DummyTexture(image.Width, image.Height);
                    break;
                case GameController.DisplayMode.Clyde:
                    _loadOpenGL(cache, path, loadParameters);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static TextureLoadParameters? _tryLoadTextureParameters(IResourceCache cache, ResourcePath path)
        {
            var metaPath = path.WithName(path.Filename + ".yml");
            if (cache.TryContentFileRead(metaPath, out var stream))
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

                return TextureLoadParameters.FromYaml((YamlMappingNode)yamlData.RootNode);
            }
            return null;
        }

        private void _loadOpenGL(IResourceCache cache, ResourcePath path, TextureLoadParameters? parameters)
        {
            DebugTools.Assert(GameController.Mode == GameController.DisplayMode.Clyde);

            var manager = IoCManager.Resolve<IClyde>();

            Texture = manager.LoadTextureFromPNGStream(cache.ContentFileRead(path), path.ToString(), parameters);
        }

        public static implicit operator Texture(TextureResource res)
        {
            return res?.Texture;
        }
    }
}
