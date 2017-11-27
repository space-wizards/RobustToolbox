using System;
using System.IO;
using SS14.Client.Graphics.Utility;
using SS14.Shared.Maths;
using SImage = SFML.Graphics.Image;

namespace SS14.Client.Graphics.Textures
{
    public class Image : IDisposable
    {
        public SImage SFMLImage { get; }

        public Vector2u Size => SFMLImage.Size.Convert();

        public Color this[uint x, uint y]
        {
            get => SFMLImage.GetPixel(x, y).Convert();
            set => SFMLImage.SetPixel(x, y, value.Convert());
        }

        public void FlipHorizontally()
        {
            SFMLImage.FlipHorizontally();
        }

        public void FlipVertically()
        {
            SFMLImage.FlipVertically();
        }

        internal Image(SImage image)
        {
            SFMLImage = image;
        }

        public Image(Stream stream)
        {
            if (!stream.CanSeek || !stream.CanRead)
            {
                throw new ArgumentException("Must be able to seek and read from stream.", nameof(stream));
            }
            SFMLImage = new SImage(stream);
        }

        public void Dispose() => SFMLImage.Dispose();

        public void SaveToFile(string filename)
        {
            if (!SFMLImage.SaveToFile(filename))
            {
                // Can't get any more accurate because of SFML, sadly.
                throw new IOException("Unable to save image to file.");
            }
        }
    }
}
