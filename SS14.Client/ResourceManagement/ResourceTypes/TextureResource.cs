using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using System.IO;

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
            godotTexture.SetFlags(godotTexture.GetFlags() & ~(int)Godot.Texture.FlagsEnum.Filter);
            Texture = new GodotTextureSource(godotTexture);
            // Primarily for tracking down iCCP sRGB errors in the image files.
            Logger.DebugS("res.tex", $"Loaded texture {path}.");
        }

        public static implicit operator Texture(TextureResource res)
        {
            return res?.Texture;
        }

        public override void Dispose()
        {
            godotTexture.Dispose();
            godotTexture = null;
        }
    }
}
