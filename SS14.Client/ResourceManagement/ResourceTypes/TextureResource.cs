using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Log;
using System.IO;

namespace SS14.Client.ResourceManagement
{
    public class TextureResource : BaseResource
    {
        public override string Fallback => "Textures/noSprite.png";
        public Texture Texture { get; private set; }
        private Godot.ImageTexture godotTexture;

        public override void Load(IResourceCache cache, string diskPath)
        {
            if (!File.Exists(diskPath))
            {
                throw new FileNotFoundException(diskPath);
            }
            godotTexture = new Godot.ImageTexture();
            godotTexture.Load(diskPath);
            // If it fails to load it won't change the texture dimensions, so they'll still be at zero.
            if (godotTexture.GetWidth() == 0)
            {
                throw new InvalidDataException();
            }
            // Disable filter by default because pixel art.
            godotTexture.SetFlags(godotTexture.GetFlags() & ~(int)Godot.Texture.FlagsEnum.Filter);
            Texture = new GodotTextureSource(godotTexture);
            // Primarily for tracking down iCCP sRGB errors in the image files.
            Logger.Debug($"Loaded texture {Path.GetFullPath(diskPath)}.");
        }

        public static implicit operator Texture(TextureResource res)
        {
            return res.Texture;
        }

        public override void Dispose()
        {
            godotTexture.Dispose();
            godotTexture = null;
        }
    }
}
