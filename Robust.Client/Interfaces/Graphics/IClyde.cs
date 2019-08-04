using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.Interfaces.Graphics
{
    public interface IClyde
    {
        Vector2i ScreenSize { get; set; }
        void SetWindowTitle(string title);
        event Action<WindowResizedEventArgs> OnWindowResized;

        Texture LoadTextureFromPNGStream(Stream stream, string name = null,
            TextureLoadParameters? loadParams = null);

        Texture LoadTextureFromImage<T>(Image<T> image, string name = null,
            TextureLoadParameters? loadParams = null) where T : unmanaged, IPixel<T>;

        IRenderTarget CreateRenderTarget(Vector2i size, RenderTargetColorFormat colorFormat,
            TextureSampleParameters? sampleParameters = null, string name = null);
    }

    public interface IClydeAudio
    {
        // AUDIO SYSTEM DOWN BELOW.
        AudioStream LoadAudioOggVorbis(Stream stream, string name = null);
        AudioStream LoadAudioWav(Stream stream, string name = null);

        IClydeAudioSource CreateAudioSource(AudioStream stream);
    }

    internal interface IClydeInternal : IClyde
    {
        // Basic main loop hooks.
        void Render();
        void FrameProcess(FrameEventArgs eventArgs);
        void ProcessInput(FrameEventArgs frameEventArgs);

        // Init.
        void Initialize(bool lite=false);
        void Ready();

        ClydeHandle LoadShader(ParsedShader shader, string name = null);
        ShaderInstance InstanceShader(ClydeHandle handle);

        /// <summary>
        ///     This is purely a hook for <see cref="IInputManager"/>, use that instead.
        /// </summary>
        Vector2 MouseScreenPosition { get; }

        IClydeDebugInfo DebugInfo { get; }

        /// <summary>
        ///     Gets the platform specific window handle exposed by OpenTK.
        ///     Seriously please avoid using this unless absolutely necessary.
        /// </summary>
        IntPtr GetNativeWindowHandle();
    }

    public interface IClydeAudioSource : IDisposable
    {
        void StartPlaying();
        bool IsPlaying { get; }
        void SetPosition(Vector2 position);
        void SetPitch(float pitch);
        void SetGlobal();
        void SetVolume(float decibels);
    }

    // TODO: Maybe implement IDisposable for render targets. I got lazy and didn't.
    /// <summary>
    ///     Represents a render target that can be drawn to.
    /// </summary>
    public interface IRenderTarget
    {
        /// <summary>
        ///     The size of the render target, in pixels.
        /// </summary>
        Vector2i Size { get; }

        /// <summary>
        ///     A texture that contains the contents of the render target.
        /// </summary>
        Texture Texture { get; }

        /// <summary>
        ///     Delete this render target and its backing textures.
        /// </summary>
        void Delete();
    }

    internal interface IRenderHandle
    {
        DrawingHandleScreen DrawingHandleScreen { get; }
        DrawingHandleWorld DrawingHandleWorld { get; }

        void SetScissor(UIBox2i? scissorBox);
        void DrawEntity(IEntity entity, Vector2 position, Vector2 scale);
    }

    /// <summary>
    ///     Formats for the color component of a render target.
    /// </summary>
    public enum RenderTargetColorFormat
    {
        /// <summary>
        ///     8 bits per channel linear RGBA.
        /// </summary>
        Rgba8,

        /// <summary>
        ///     8 bits per channel sRGB with linear alpha channel.
        /// </summary>
        Rgba8Srgb,

        /// <summary>
        ///     16 bits per channel floating point linear RGBA.
        /// </summary>
        Rgba16F
    }

    public class WindowResizedEventArgs : EventArgs
    {
        public WindowResizedEventArgs(Vector2i oldSize, Vector2i newSize)
        {
            OldSize = oldSize;
            NewSize = newSize;
        }

        public Vector2i OldSize { get; }
        public Vector2i NewSize { get; }
    }

    public interface IClydeDebugInfo
    {
        Version OpenGLVersion { get; }
        Version MinimumVersion { get; }

        string Renderer { get; }
        string Vendor { get; }
        string VersionString { get; }
    }
}
