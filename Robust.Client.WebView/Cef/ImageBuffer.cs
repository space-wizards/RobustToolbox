using System;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xilium.CefGlue;

namespace Robust.Client.WebView.Cef
{
    internal sealed class ImageBuffer
    {
        public Image<Rgba32> Buffer { get; private set; } = new(1, 1);

        public unsafe void UpdateBuffer(int width, int height, IntPtr buffer, CefRectangle dirtyRect)
        {
            if (width != Buffer.Width || height != Buffer.Height)
                UpdateSize(width, height);

            // NOTE: Image data from CEF is actually BGRA32, not RGBA32.
            // OpenGL ES does not allow uploading BGRA data, so we pretend it's RGBA32 and use a shader to swizzle it.
            var span = new ReadOnlySpan<Rgba32>((void*) buffer, width * height);

            ImageSharpExt.Blit(
                span,
                width,
                UIBox2i.FromDimensions(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height),
                Buffer,
                (dirtyRect.X, dirtyRect.Y));
        }

        private void UpdateSize(int width, int height)
        {
            Buffer = new Image<Rgba32>(width, height);
        }
    }
}
