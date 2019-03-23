using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SS14.Client.Audio;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Clyde;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.Input;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Graphics
{
    internal interface IClyde : IDisplayManager
    {
        void Render(FrameEventArgs args);
        void FrameProcess(RenderFrameEventArgs eventArgs);
        void ProcessInput(FrameEventArgs frameEventArgs);

        Texture LoadTextureFromPNGStream(Stream stream, string name=null,
            TextureLoadParameters? loadParams = null);
        Texture LoadTextureFromImage<T>(Image<T> image, string name=null,
            TextureLoadParameters? loadParams = null) where T : unmanaged, IPixel<T>;
        TextureArray LoadArrayFromImages<T>(ICollection<Image<T>> images, string name = null,
            TextureLoadParameters? loadParams = null)
            where T : unmanaged, IPixel<T>;

        int LoadShader(ParsedShader shader, string name = null);

        void Ready();

        /// <summary>
        ///     This is purely a hook for <see cref="IInputManager"/>, use that instead.
        /// </summary>
        Vector2 MouseScreenPosition { get; }

        // AUDIO SYSTEM DOWN BELOW.
        AudioStream LoadAudioOggVorbis(Stream stream);
        AudioStream LoadAudioWav(Stream stream);

        IClydeAudioSource CreateAudioSource(AudioStream stream);

        /// <summary>
        ///     Gets the platform specific window handle exposed by OpenTK.
        ///     Seriously please avoid using this unless absolutely necessary.
        /// </summary>
        IntPtr GetNativeWindowHandle();
    }

    internal interface IClydeAudioSource : IDisposable
    {
        void StartPlaying();
        bool IsPlaying { get; }
        void SetPosition(Vector2 position);
        void SetPitch(float pitch);
        void SetGlobal();
        void SetVolume(float decibels);
    }
}
