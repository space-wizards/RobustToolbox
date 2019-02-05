using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Input;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Graphics
{
    internal interface IDisplayManagerOpenGL : IDisplayManager
    {
        void Render(FrameEventArgs args);
        void ProcessInput(FrameEventArgs frameEventArgs);

        Texture LoadTextureFromPNGStream(Stream stream, string name=null);
        Texture LoadTextureFromImage<T>(Image<T> image, string name=null) where T : unmanaged, IPixel<T>;
        TextureArray LoadArrayFromImages<T>(ICollection<Image<T>> images, string name = null)
            where T : unmanaged, IPixel<T>;

        void Ready();

        /// <summary>
        ///     This is purely a hook for <see cref="IInputManager"/>, use that instead.
        /// </summary>
        Vector2 MouseScreenPosition { get; }
    }
}
