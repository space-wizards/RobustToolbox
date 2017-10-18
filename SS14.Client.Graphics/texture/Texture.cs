using SS14.Client.Graphics.Sprites;
using SS14.Shared.Maths;
using SS14.Client.Graphics.Utility;
using STexture = SFML.Graphics.Texture;
using System.IO;
using System;

namespace SS14.Client.Graphics.Textures
{
    public class Texture : IDisposable
    {
        public STexture SFMLTexture { get; }
        public Vector2u Size => SFMLTexture.Size.Convert();

        public Texture(uint width, uint height)
        {
            SFMLTexture = new STexture(width, height);
        }

        public Texture(Stream stream)
        {
            if (!stream.CanSeek || !stream.CanSeek)
            {
                throw new ArgumentException("Stream must be read and seekable.", nameof(stream));
            }

            SFMLTexture = new STexture(stream);
        }

        public Texture(Image image)
        {
            SFMLTexture = new STexture(image.SFMLImage);
        }

        public Texture(byte[] bytes)
        {
            SFMLTexture = new STexture(bytes);
        }

        internal Texture(STexture sfmlTexture)
        {
            SFMLTexture = sfmlTexture;
        }

        public void Dispose() => SFMLTexture.Dispose();
    }
}
