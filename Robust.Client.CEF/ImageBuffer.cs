using System;
using Robust.Client.UserInterface;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xilium.CefGlue;

namespace Robust.Client.CEF
{
    internal sealed class ImageBuffer
    {
        private readonly Control _control;

        public ImageBuffer(Control control)
        {
            _control = control;
        }

        public Image<Bgra32> Buffer { get; private set; } = new(1, 1);

        public unsafe void UpdateBuffer(int width, int height, IntPtr buffer, CefRectangle dirtyRect)
        {
            if (width != Buffer.Width || height != Buffer.Height)
                UpdateSize(width, height);

            var span = new ReadOnlySpan<Bgra32>((void*) buffer, width * height);

            ImageSharpExt.Blit(
                span,
                width,
                UIBox2i.FromDimensions(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height),
                Buffer,
                (dirtyRect.X, dirtyRect.Y));
        }

        private void UpdateSize(int width, int height)
        {
            Buffer = new Image<Bgra32>(width, height);
        }
    }
}
