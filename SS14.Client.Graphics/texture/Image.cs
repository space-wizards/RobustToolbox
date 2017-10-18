using System;
using SS14.Client.Graphics.Utility;
using SS14.Shared.Maths;
using SImage = SFML.Graphics.Image;

namespace SS14.Client.Graphics.Textures
{
    public class Image : IDisposable
    {
        public SImage SFMLImage { get; private set; }

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

        public void Dispose() => SFMLImage.Dispose();
    }
}
