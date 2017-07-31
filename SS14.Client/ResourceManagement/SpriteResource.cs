using System.IO;
using SFML.Graphics;
using SS14.Client.ResourceManagment;
using SS14.Client.Resources;

namespace SS14.Client.ResourceManagement
{
    /// <summary>
    ///     Holds a SFML Sprite resource in the cache.
    /// </summary>
    public class SpriteResource : BaseResource
    {
        /// <summary>
        /// The contained SFML Sprite.
        /// </summary>
        public Sprite Sprite { get; private set; }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override void Dispose()
        {
            Sprite.Dispose();
        }
    }
}
