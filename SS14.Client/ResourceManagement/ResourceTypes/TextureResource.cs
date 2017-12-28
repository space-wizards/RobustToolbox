using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Log;
using System.IO;

namespace SS14.Client.ResourceManagement
{
    public class TextureResource : BaseResource
    {
        public TextureSource Texture { get; private set; }
        private Godot.ImageTexture texture;

        public override void Load(IResourceCache cache, string diskPath)
        {
            if (!System.IO.File.Exists(diskPath))
            {
                throw new FileNotFoundException(diskPath);
            }
            texture = new Godot.ImageTexture();
            texture.Load(diskPath);
            // If it fails to load it won't change the texture dimensions, so they'll still be at zero.
            if (texture.GetWidth() == 0)
            {
                throw new InvalidDataException();
            }
            // Disable filter by default because pixel art.
            texture.SetFlags(texture.GetFlags() & ~Godot.Texture.FLAG_FILTER);
            Texture = new GodotTextureSource(texture);
            // Primarily for tracking down iCCP sRGB errors in the image files.
            Logger.Debug($"Loaded texture {Path.GetFullPath(diskPath)}");
        }

        public override void Dispose()
        {
            texture.Free();
        }
    }
}
