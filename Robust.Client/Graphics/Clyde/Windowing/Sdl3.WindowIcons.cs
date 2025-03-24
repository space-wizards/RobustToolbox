using System;
using System.Linq;
using System.Runtime.InteropServices;
using Robust.Client.Utility;
using SDL3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Windows;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed unsafe partial class Sdl3WindowingImpl
    {
        // Experimentally on my system, SM_CXICON is 32.
        // I doubt MS is ever changing that, so...
        // I wish SDL would take care of this instead of us having to figure out what the "main" icon is. Ugh.
        private const int MainWindowIconSize = 32;

        // Writing this out like this makes me realize we're spending multiple hundred KBs on storing the window icon.
        // You know, come to think about it, what if we used LZ4 or Zstd to compress the window icon stored here?
        // This is absolutely not worth the optimization but hilarious for me to think about.

        // The surface used for the window icon.
        // This may store additional surfaces as alternate forms.
        private nint _windowIconSurface;
        // The data for all the window icons surfaces.
        // Must be kept around! Pinned!
        // ReSharper disable once CollectionNeverQueried.Local
        private byte[][]? _windowIconData;

        private void LoadWindowIcons()
        {
            // Sort such that closest to 64 is first.
            // SDL3 doesn't "figure it out itself" as much as GLFW does, which sucks.
            var icons = _clyde.LoadWindowIcons().OrderBy(i => Math.Abs(i.Width - MainWindowIconSize)).ToArray();
            if (icons.Length == 0)
            {
                // No window icons at all!
                return;
            }

            _windowIconData = new byte[icons.Length][];

            var mainImg = icons[0];

            _sawmill.Verbose(
                "Have {iconCount} window icons available, choosing {mainIconWidth}x{mainIconHeight} as main",
                icons.Length,
                mainImg.Width,
                mainImg.Height);

            (_windowIconSurface, var mainData) = CreateSurfaceFromImage(mainImg);
            _windowIconData[0] = mainData;

            for (var i = 1; i < icons.Length; i++)
            {
                var (surface, data) = CreateSurfaceFromImage(icons[i]);
                _windowIconData[i] = data;
                SDL.SDL_AddSurfaceAlternateImage(_windowIconSurface, surface);
                // Kept alive by the main surface.
                SDL.SDL_DestroySurface(surface);
            }

            return;

            static (nint, byte[]) CreateSurfaceFromImage(Image<Rgba32> img)
            {
                var span = MemoryMarshal.AsBytes(img.GetPixelSpan());
                var copied = GC.AllocateUninitializedArray<byte>(span.Length, pinned: true);

                span.CopyTo(copied);

                IntPtr surface;
                fixed (byte* ptr = copied)
                {
                    surface = SDL.SDL_CreateSurfaceFrom(
                        img.Width,
                        img.Height,
                        SDL.SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888,
                        (IntPtr)ptr,
                        sizeof(Rgba32) * img.Width);
                }

                return (surface, copied);
            }
        }

        private void DestroyWindowIcons()
        {
            SDL.SDL_DestroySurface(_windowIconSurface);
            _windowIconSurface = 0;
            _windowIconData = null;
        }

        private void AssignWindowIconToWindow(nint window)
        {
            if (_windowIconSurface == 0)
                return;

            SDL.SDL_SetWindowIcon(window, (nint) _windowIconSurface);
        }
    }
}
