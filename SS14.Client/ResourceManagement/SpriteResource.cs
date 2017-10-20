using System.IO;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Resource;
using SS14.Client.ResourceManagment;

namespace SS14.Client.ResourceManagement
{
    /// <summary>
    ///     Holds a SFML Sprite resource in the cache.
    /// </summary>
    public class SpriteResource : BaseResource
    {
        /// <inheritdoc />
        public override string Fallback => @"Textures/noSprite.png";

        /// <summary>
        /// The contained SFML Sprite.
        /// </summary>
        public Sprite Sprite { get; private set; }

        /// <inheritdoc />
        public override void Load(IResourceCache cache, string path, Stream stream)
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

        public static implicit operator Sprite(SpriteResource res)
        {
            return res.Sprite;
        }
    }
}
