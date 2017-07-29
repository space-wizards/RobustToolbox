using System.IO;
using SFML.Graphics;
using SS14.Client.ResourceManagement;
using SS14.Client.Resources;

namespace SS14.Client.ResourceManagment
{
    class TextureResource : BaseResource
    {
        public override string Fallback => @"Textures/noSprite.png";

        public Texture Texture { get; private set; }

        public override void Load(ResourceCache cache, string path, Stream stream)
        {
            Texture = new Texture(stream);
        }

        public override void Dispose()
        {
            Texture.Dispose();
        }
    }
}
