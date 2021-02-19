using System;
using System.IO;
using System.Threading.Tasks;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Graphics
{
    public interface IClyde
    {
        IRenderWindow MainWindowRenderTarget { get; }

        Vector2i ScreenSize { get; }

        /// <summary>
        ///     The default scale ratio for window contents, given to us by the OS.
        /// </summary>
        Vector2 DefaultWindowScale { get; }

        void SetWindowTitle(string title);

        /// <summary>
        ///     This is the magic method to make the game window ping you in the task bar.
        /// </summary>
        void RequestWindowAttention();

        event Action<WindowResizedEventArgs> OnWindowResized;

        event Action<WindowFocusedEventArgs> OnWindowFocused;

        Texture LoadTextureFromPNGStream(Stream stream, string? name = null,
            TextureLoadParameters? loadParams = null);

        Texture LoadTextureFromImage<T>(Image<T> image, string? name = null,
            TextureLoadParameters? loadParams = null) where T : unmanaged, IPixel<T>;

        /// <summary>
        ///     Creates a blank texture of the specified parameters.
        ///     This texture can later be modified using <see cref="OwnedTexture.SetSubImage{T}"/>
        /// </summary>
        /// <param name="size">The size of the new texture, in pixels.</param>
        /// <param name="name">A name for the texture that can show up in debugging tools like renderdoc.</param>
        /// <param name="loadParams">
        ///     Load parameters for the texture describing stuff such as sample mode.
        /// </param>
        /// <typeparam name="T">
        ///     The type of pixels to "store" in the texture.
        ///     This is the same type you should pass to <see cref="OwnedTexture.SetSubImage{T}"/>,
        ///     and also determines how the texture is stored internally.
        /// </typeparam>
        /// <returns>
        ///     An owned, mutable texture object.
        /// </returns>
        OwnedTexture CreateBlankTexture<T>(
            Vector2i size,
            string? name = null,
            in TextureLoadParameters? loadParams = null)
            where T : unmanaged, IPixel<T>;

        IRenderTexture CreateRenderTarget(Vector2i size, RenderTargetFormatParameters format,
            TextureSampleParameters? sampleParameters = null, string? name = null);

        // Cursor API.
        /// <summary>
        ///     Gets a cursor object representing standard cursors that match the OS styling.
        /// </summary>
        /// <remarks>
        ///     Cursor objects returned from this method are cached and you cannot not dispose them.
        /// </remarks>
        ICursor GetStandardCursor(StandardCursorShape shape);

        /// <summary>
        ///     Create a custom cursor object from an image.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="hotSpot"></param>
        /// <returns></returns>
        ICursor CreateCursor(Image<Rgba32> image, Vector2i hotSpot);

        /// <summary>
        ///     Sets the active cursor for the primary window.
        /// </summary>
        /// <param name="cursor">The cursor to set to, or <see langword="null"/> to reset to the default cursor.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the cursor object passed has been disposed.</exception>
        void SetCursor(ICursor? cursor);

        /// <summary>
        ///     Make a screenshot of the game, next render frame.
        /// </summary>
        /// <param name="type">What kind of screenshot to take</param>
        /// <param name="callback">The callback to run when the screenshot has been made.</param>
        void Screenshot(ScreenshotType type, Action<Image<Rgb24>> callback);

        Task<Image<Rgb24>> ScreenshotAsync(ScreenshotType type)
        {
            var tcs = new TaskCompletionSource<Image<Rgb24>>();

            Screenshot(type, image => tcs.SetResult(image));

            return tcs.Task;
        }

        IClydeViewport CreateViewport(Vector2i size, string? name = null);
    }

    // TODO: Maybe implement IDisposable for render targets. I got lazy and didn't.
}
