using System;
using System.IO;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.ResourceManagement.ResourceTypes
{
    public class TextureResource : BaseResource
    {
        public override ResourcePath Fallback { get; } = new ResourcePath("/Textures/noSprite.png");
        public Texture Texture { get; private set; }
        private Godot.ImageTexture godotTexture;

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
                    Texture = new DummyTexture();
                    break;
                case GameController.DisplayMode.Godot:
                    _loadGodot(cache, path, loadParameters);
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

        private void _loadGodot(IResourceCache cache, ResourcePath path, TextureLoadParameters? parameters)
        {
            DebugTools.Assert((bool) (GameController.Mode == GameController.DisplayMode.Godot));

            using (var stream = cache.ContentFileRead(path))
            {
                var buffer = stream.ToArray();
                var image = new Godot.Image();
                var error = image.LoadPngFromBuffer(buffer);
                if (error != Godot.Error.Ok)
                {
                    throw new InvalidDataException($"Unable to load texture from buffer, reason: {error}");
                }
                godotTexture = new Godot.ImageTexture();
                godotTexture.CreateFromImage(image);
            }

            // Disable filter by default because pixel art.
            (parameters ?? TextureLoadParameters.Default).SampleParameters.ApplyToGodotTexture(godotTexture);
            Texture = new GodotTextureSource(godotTexture);
        }

        private void _loadOpenGL(IResourceCache cache, ResourcePath path, TextureLoadParameters? parameters)
        {
            DebugTools.Assert((bool) (GameController.Mode == GameController.DisplayMode.Clyde));

            var manager = IoCManager.Resolve<IClyde>();

            Texture = manager.LoadTextureFromPNGStream(cache.ContentFileRead(path), path.ToString(), parameters);
        }

        public static implicit operator Texture(TextureResource res)
        {
            return res?.Texture;
        }

        public override void Dispose()
        {
            if (GameController.OnGodot)
            {
                godotTexture.Dispose();
            }
        }
    }
}
