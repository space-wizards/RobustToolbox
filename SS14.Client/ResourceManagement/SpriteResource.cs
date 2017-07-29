using System.IO;
using SFML.Graphics;
using SS14.Client.ResourceManagment;
using SS14.Client.Resources;

namespace SS14.Client.ResourceManagement
{
    class SpriteResource : BaseResource
    {
        public Sprite Sprite { get; private set; }

        public override void Load(ResourceCache cache, string path, Stream stream)
        {
            if (!cache.TryGetResource(path, out TextureResource res))
            {
                res = new TextureResource();
                res.Load(cache, path, stream);
                cache.CacheResource(path, res);
            }

            Sprite = new Sprite(res.Texture);
        }

        public override void Dispose()
        {
            Sprite.Dispose();
        }
    }
}
