using System;
using System.IO;
using Robust.Client.Audio;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Graphics.Clyde
{
    /// <summary>
    ///     Hey look, it's Clyde's evil twin brother!
    /// </summary>
    internal sealed class ClydeHeadless : DisplayManager, IClyde
    {
        // Would it make sense to report a fake resolution like 720p here so code doesn't break? idk.
        public override Vector2i ScreenSize => (1280, 720);
        public Vector2 MouseScreenPosition => ScreenSize / 2;

        public override void SetWindowTitle(string title)
        {
            // Nada.
        }

        public override void Initialize()
        {
            // Nada.
        }

#pragma warning disable CS0067
        public override event Action<WindowResizedEventArgs> OnWindowResized;
#pragma warning restore CS0067

        public void Render(FrameEventArgs args)
        {
            // Nada.
        }

        public void FrameProcess(RenderFrameEventArgs eventArgs)
        {
            // Nada.
        }

        public void ProcessInput(FrameEventArgs frameEventArgs)
        {
            // Nada.
        }

        public Texture LoadTextureFromPNGStream(Stream stream, string name = null,
            TextureLoadParameters? loadParams = null)
        {
            using (var image = Image.Load(stream))
            {
                return LoadTextureFromImage(image, name, loadParams);
            }
        }

        public Texture LoadTextureFromImage<T>(Image<T> image, string name = null,
            TextureLoadParameters? loadParams = null) where T : unmanaged, IPixel<T>
        {
            return new DummyTexture(image.Width, image.Height);
        }

        public int LoadShader(ParsedShader shader, string name = null)
        {
            return default;
        }

        public void Ready()
        {
            // Nada.
        }

        public AudioStream LoadAudioOggVorbis(Stream stream, string name = null)
        {
            // TODO: Might wanna actually load this so the length gets reported correctly.
            return new AudioStream(default, default, 1, name);
        }

        public AudioStream LoadAudioWav(Stream stream, string name = null)
        {
            // TODO: Might wanna actually load this so the length gets reported correctly.
            return new AudioStream(default, default, 1, name);
        }

        public IClydeAudioSource CreateAudioSource(AudioStream stream)
        {
            return DummyAudioSource.Instance;
        }

        public IntPtr GetNativeWindowHandle()
        {
            return default;
        }

        private sealed class DummyAudioSource : IClydeAudioSource
        {
            public static DummyAudioSource Instance { get; } = new DummyAudioSource();
            public bool IsPlaying => default;

            public void Dispose()
            {
                // Nada.
            }

            public void StartPlaying()
            {
                // Nada.
            }

            public void SetPosition(Vector2 position)
            {
                // Nada.
            }

            public void SetPitch(float pitch)
            {
                // Nada.
            }

            public void SetGlobal()
            {
                // Nada.
            }

            public void SetVolume(float decibels)
            {
                // Nada.
            }
        }
    }
}
