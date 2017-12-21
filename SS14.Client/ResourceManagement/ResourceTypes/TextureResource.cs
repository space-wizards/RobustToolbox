using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Log;

namespace SS14.Client.ResourceManagement
{
    public class TextureResource : BaseResource
    {
        public Texture Texture => texture;
        private ImageTexture texture;

        public override void Load(IResourceCache cache, string diskPath)
        {
            if (!System.IO.File.Exists(diskPath))
            {
                throw new FileNotFoundException(diskPath);
            }
            texture = new ImageTexture();
            texture.Load(diskPath);
            // If it fails to load it won't change the texture dimensions, so they'll still be at zero.
            if (texture.GetWidth() == 0)
            {
                throw new InvalidDataException();
            }
            Logger.Debug($"Loaded texture {diskPath}. w: {texture.GetWidth()}, h: {texture.GetWidth()}");
            // Disable filter by default because pixel art.
            texture.SetFlags(texture.GetFlags() & ~Texture.FLAG_FILTER);
        }

        public override void Dispose()
        {
            texture.Free();
        }
    }
}
