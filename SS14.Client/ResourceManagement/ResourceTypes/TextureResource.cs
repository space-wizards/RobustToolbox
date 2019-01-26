using System;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using System.IO;
using SS14.Client.Interfaces.Graphics;
using SS14.Shared.IoC;

namespace SS14.Client.ResourceManagement
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

            switch (GameController.Mode)
            {
                case GameController.DisplayMode.Headless:
                    Texture = new BlankTexture();
                    break;
                case GameController.DisplayMode.Godot:
                    _loadGodot(cache, path);
                    break;
                case GameController.DisplayMode.OpenGL:
                    _loadOpenGL(cache, path);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void _loadGodot(IResourceCache cache, ResourcePath path)
        {
            DebugTools.Assert(GameController.Mode == GameController.DisplayMode.Godot);

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
            godotTexture.SetFlags(godotTexture.GetFlags() & ~(int) Godot.Texture.FlagsEnum.Filter);
            Texture = new GodotTextureSource(godotTexture);
        }

        private void _loadOpenGL(IResourceCache cache, ResourcePath path)
        {
            DebugTools.Assert(GameController.Mode == GameController.DisplayMode.OpenGL);

            var manager = IoCManager.Resolve<IDisplayManagerOpenGL>();

            Texture = manager.LoadTextureFromPNGStream(cache.ContentFileRead(path), path.ToString());
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
