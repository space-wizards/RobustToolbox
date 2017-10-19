using System.IO;
using SFML.Graphics;
using SS14.Client.Interfaces.Resource;

namespace SS14.Client.ResourceManagement
{
    /// <summary>
    ///     Holds a SFML Font resource in the cache.
    /// </summary>
    public class FontResource : BaseResource
    {
        public override string Fallback => @"Fonts/bluehigh.ttf";

        /// <summary>
        ///     The contained font.
        /// </summary>
        public Font Font { get; private set; }

        /// <inheritdoc />
        public override void Load(IResourceCache cache, string path, Stream stream)
        {
            Font = new Font(stream);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Font.Dispose();
            Font = null;
        }

        public static implicit operator Font(FontResource res)
        {
            return res.Font;
        }
    }
}
