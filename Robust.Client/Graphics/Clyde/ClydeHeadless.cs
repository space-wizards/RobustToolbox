using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Input;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Robust.Client.Graphics.Clyde
{
    /// <summary>
    ///     Hey look, it's Clyde's evil twin brother!
    /// </summary>
    [UsedImplicitly]
    internal sealed class ClydeHeadless : IClydeInternal, IClydeAudio
    {
        // Would it make sense to report a fake resolution like 720p here so code doesn't break? idk.
        public IClydeWindow MainWindow { get; }
        public Vector2i ScreenSize => (1280, 720);
        public IEnumerable<IClydeWindow> AllWindows => _windows;
        public Vector2 DefaultWindowScale => (1, 1);
        public bool IsFocused => true;
        private readonly List<IClydeWindow> _windows = new();

        public ShaderInstance InstanceShader(ClydeHandle handle)
        {
            return new DummyShaderInstance();
        }

        public ClydeHeadless()
        {
            var mainRt = new DummyRenderWindow(this);
            var window = new DummyWindow(mainRt);

            _windows.Add(window);
            MainWindow = window;
        }

        public Vector2 MouseScreenPosition => ScreenSize / 2;
        public IClydeDebugInfo DebugInfo { get; } = new DummyDebugInfo();
        public IClydeDebugStats DebugStats { get; } = new DummyDebugStats();

        public event Action<TextEventArgs>? TextEntered;
        public event Action<MouseMoveEventArgs>? MouseMove;
        public event Action<KeyEventArgs>? KeyUp;
        public event Action<KeyEventArgs>? KeyDown;
        public event Action<MouseWheelEventArgs>? MouseWheel;
        public event Action<WindowClosedEventArgs>? CloseWindow;

        public Texture GetStockTexture(ClydeStockTexture stockTexture)
        {
            return new DummyTexture((1, 1));
        }

        public ClydeDebugLayers DebugLayers { get; set; }

        public string GetKeyName(Keyboard.Key key) => string.Empty;

        public void Shutdown()
        {
            // Nada.
        }

        public uint? GetX11WindowId()
        {
            return null;
        }

        public void SetWindowTitle(string title)
        {
            // Nada.
        }

        public void SetWindowMonitor(IClydeMonitor monitor)
        {
            // Nada.
        }

        public void RequestWindowAttention()
        {
            // Nada.
        }

        public event Action<WindowResizedEventArgs> OnWindowResized
        {
            add { }
            remove { }
        }

        public event Action<WindowFocusedEventArgs> OnWindowFocused
        {
            add { }
            remove { }
        }

        public event Action OnWindowScaleChanged
        {
            add { }
            remove { }
        }

        public void Render()
        {
            // Nada.
        }

        public void FrameProcess(FrameEventArgs eventArgs)
        {
            // Nada.
        }

        public void ProcessInput(FrameEventArgs frameEventArgs)
        {
            // Nada.
        }

        public bool SeparateWindowThread => false;

        public bool InitializePreWindowing()
        {
            return true;
        }

        public void TerminateWindowLoop()
        {
            throw new InvalidOperationException("ClydeHeadless does not use windowing threads");
        }

        public void EnterWindowLoop()
        {
            throw new InvalidOperationException("ClydeHeadless does not use windowing threads");
        }

        public bool InitializePostWindowing()
        {
            return true;
        }

        public OwnedTexture LoadTextureFromPNGStream(Stream stream, string? name = null,
            TextureLoadParameters? loadParams = null)
        {
            using (var image = Image.Load<Rgba32>(stream))
            {
                return LoadTextureFromImage(image, name, loadParams);
            }
        }

        public OwnedTexture LoadTextureFromImage<T>(Image<T> image, string? name = null,
            TextureLoadParameters? loadParams = null) where T : unmanaged, IPixel<T>
        {
            return new DummyTexture((image.Width, image.Height));
        }

        public OwnedTexture CreateBlankTexture<T>(
            Vector2i size,
            string? name = null,
            in TextureLoadParameters? loadParams = null)
            where T : unmanaged, IPixel<T>
        {
            return new DummyTexture(size);
        }

        public IRenderTexture CreateRenderTarget(Vector2i size, RenderTargetFormatParameters format,
            TextureSampleParameters? sampleParameters = null, string? name = null)
        {
            return new DummyRenderTexture(size, new DummyTexture(size));
        }

        public ICursor GetStandardCursor(StandardCursorShape shape)
        {
            return new DummyCursor();
        }

        public ICursor CreateCursor(Image<Rgba32> image, Vector2i hotSpot)
        {
            return new DummyCursor();
        }

        public void SetCursor(ICursor? cursor)
        {
            // Nada.
        }

        public void Screenshot(ScreenshotType type, CopyPixelsDelegate<Rgb24> callback, UIBox2i? subRegion = null)
        {
            // Immediately call callback with an empty buffer.
            var (x, y) = ClydeBase.ClampSubRegion(ScreenSize, subRegion);
            callback(new Image<Rgb24>(x, y));
        }

        public IClydeViewport CreateViewport(Vector2i size, TextureSampleParameters? sampleParameters,
            string? name = null)
        {
            return new Viewport(size);
        }

        public IEnumerable<IClydeMonitor> EnumerateMonitors()
        {
            // TODO: Actually return something.
            yield break;
        }

        public Task<IClydeWindow> CreateWindow()
        {
            var window = new DummyWindow(CreateRenderTarget((123, 123), default));
            _windows.Add(window);

            return Task.FromResult<IClydeWindow>(window);
        }

        public ClydeHandle LoadShader(ParsedShader shader, string? name = null)
        {
            return default;
        }

        public void ReloadShader(ClydeHandle handle, ParsedShader newShader)
        {
            // Nada.
        }

        public void Ready()
        {
            // Nada.
        }

        public AudioStream LoadAudioOggVorbis(Stream stream, string? name = null)
        {
            // TODO: Might wanna actually load this so the length gets reported correctly.
            return new(default, default, 1, name);
        }

        public AudioStream LoadAudioWav(Stream stream, string? name = null)
        {
            // TODO: Might wanna actually load this so the length gets reported correctly.
            return new(default, default, 1, name);
        }

        public IClydeAudioSource CreateAudioSource(AudioStream stream)
        {
            return DummyAudioSource.Instance;
        }

        public IClydeBufferedAudioSource CreateBufferedAudioSource(int buffers, bool floatAudio = false)
        {
            return DummyBufferedAudioSource.Instance;
        }

        public Task<string> GetText()
        {
            return Task.FromResult(string.Empty);
        }

        public void SetText(string text)
        {
            // Nada.
        }

        public void SetMasterVolume(float newVolume)
        {
            // Nada.
        }

        private class DummyCursor : ICursor
        {
            public void Dispose()
            {
                // Nada.
            }
        }

        private class DummyAudioSource : IClydeAudioSource
        {
            public static DummyAudioSource Instance { get; } = new();

            public bool IsPlaying => default;
            public bool IsLooping { get; set; }

            public void Dispose()
            {
                // Nada.
            }

            public void StartPlaying()
            {
                // Nada.
            }

            public void StopPlaying()
            {
                // Nada.
            }

            public bool SetPosition(Vector2 position)
            {
                return true;
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

            public void SetOcclusion(float blocks)
            {
                // Nada.
            }

            public void SetPlaybackPosition(float seconds)
            {
                // Nada.
            }

            public void SetVelocity(Vector2 velocity)
            {
                // Nada.
            }
        }

        private sealed class DummyBufferedAudioSource : DummyAudioSource, IClydeBufferedAudioSource
        {
            public new static DummyBufferedAudioSource Instance { get; } = new();
            public int SampleRate { get; set; } = 0;

            public void WriteBuffer(int handle, ReadOnlySpan<ushort> data)
            {
                // Nada.
            }

            public void WriteBuffer(int handle, ReadOnlySpan<float> data)
            {
                // Nada.
            }

            public void QueueBuffers(ReadOnlySpan<int> handles)
            {
                // Nada.
            }

            public void EmptyBuffers()
            {
                // Nada.
            }

            public void GetBuffersProcessed(Span<int> handles)
            {
                // Nada.
            }

            public int GetNumberOfBuffersProcessed()
            {
                return 0;
            }
        }

        private sealed class DummyTexture : OwnedTexture
        {
            public DummyTexture(Vector2i size) : base(size)
            {
            }

            public override void SetSubImage<T>(Vector2i topLeft, Image<T> sourceImage, in UIBox2i sourceRegion)
            {
                // Just do nothing on mutate.
            }
        }

        private sealed class DummyShaderInstance : ShaderInstance
        {
            private protected override ShaderInstance DuplicateImpl()
            {
                return new DummyShaderInstance();
            }

            private protected override void SetParameterImpl(string name, float value)
            {
            }

            private protected override void SetParameterImpl(string name, Vector2 value)
            {
            }

            private protected override void SetParameterImpl(string name, Vector3 value)
            {
            }

            private protected override void SetParameterImpl(string name, Vector4 value)
            {
            }

            private protected override void SetParameterImpl(string name, Color value)
            {
            }

            private protected override void SetParameterImpl(string name, int value)
            {
            }

            private protected override void SetParameterImpl(string name, Vector2i value)
            {
            }

            private protected override void SetParameterImpl(string name, bool value)
            {
            }

            private protected override void SetParameterImpl(string name, in Matrix3 value)
            {
            }

            private protected override void SetParameterImpl(string name, in Matrix4 value)
            {
            }

            private protected override void SetParameterImpl(string name, Texture value)
            {
            }

            private protected override void SetStencilOpImpl(StencilOp op)
            {
            }

            private protected override void SetStencilFuncImpl(StencilFunc func)
            {
            }

            private protected override void SetStencilTestEnabledImpl(bool enabled)
            {
            }

            private protected override void SetStencilRefImpl(int @ref)
            {
            }

            private protected override void SetStencilWriteMaskImpl(int mask)
            {
            }

            private protected override void SetStencilReadMaskRefImpl(int mask)
            {
            }
        }

        private sealed class DummyRenderTexture : IRenderTexture
        {
            public DummyRenderTexture(Vector2i size, Texture texture)
            {
                Size = size;
                Texture = texture;
            }

            public Vector2i Size { get; }

            public void CopyPixelsToMemory<T>(CopyPixelsDelegate<T> callback, UIBox2i? subRegion) where T : unmanaged, IPixel<T>
            {
                var (x, y) = ClydeBase.ClampSubRegion(Size, subRegion);
                callback(new Image<T>(x, y));
            }

            public Texture Texture { get; }

            public void Dispose()
            {
            }
        }

        private sealed class DummyRenderWindow : IRenderTarget
        {
            private readonly ClydeHeadless _clyde;

            public DummyRenderWindow(ClydeHeadless clyde)
            {
                _clyde = clyde;
            }

            public Vector2i Size => _clyde.ScreenSize;

            public void CopyPixelsToMemory<T>(CopyPixelsDelegate<T> callback, UIBox2i? subRegion) where T : unmanaged, IPixel<T>
            {
                var (x, y) = ClydeBase.ClampSubRegion(Size, subRegion);
                callback(new Image<T>(x, y));
            }

            public void Dispose()
            {
            }
        }

        private sealed class DummyDebugStats : IClydeDebugStats
        {
            public int LastGLDrawCalls => 0;
            public int LastClydeDrawCalls => 0;
            public int LastBatches => 0;
            public (int vertices, int indices) LargestBatchSize => (0, 0);
            public int TotalLights => 0;
        }

        private sealed class DummyDebugInfo : IClydeDebugInfo
        {
            public OpenGLVersion OpenGLVersion { get; } = new(3, 3, isES: false, isCore: true);
            public string Renderer => "ClydeHeadless";
            public string Vendor => "Space Wizards Federation";
            public string VersionString { get; } = $"3.3.0 WIZARDS {typeof(DummyDebugInfo).Assembly.GetName().Version}";
            public bool Overriding => false;
        }

        private sealed class Viewport : IClydeViewport
        {
            public Viewport(Vector2i size)
            {
                Size = size;
            }

            public void Dispose()
            {
            }

            public IRenderTexture RenderTarget { get; } =
                new DummyRenderTexture(Vector2i.One, new DummyTexture(Vector2i.One));

            public IEye? Eye { get; set; }
            public Vector2i Size { get; }
            public Vector2 RenderScale { get; set; }
            public bool AutomaticRender { get; set; }

            public void Render()
            {
                // Nada
            }

            public MapCoordinates LocalToWorld(Vector2 point)
            {
                return default;
            }

            public Vector2 WorldToLocal(Vector2 point)
            {
                return default;
            }

            public void RenderScreenOverlaysBelow(
                DrawingHandleScreen handle,
                IViewportControl control,
                in UIBox2i viewportBounds)
            {
                // Nada
            }

            public void RenderScreenOverlaysAbove(
                DrawingHandleScreen handle,
                IViewportControl control,
                in UIBox2i viewportBounds)
            {
                // Nada
            }
        }

        private sealed class DummyWindow : IClydeWindow
        {
            public DummyWindow(IRenderTarget renderTarget)
            {
                RenderTarget = renderTarget;
            }

            public Vector2i Size { get; } = default;
            public bool IsDisposed { get; private set; }
            public IRenderTarget RenderTarget { get; }
            public string Title { get; set; } = "";
            public bool IsFocused => false;
            public bool IsMinimized => false;
            public bool IsVisible { get; set; } = true;
            public event Action<WindowClosedEventArgs>? Closed;

            public void Dispose()
            {
                Closed?.Invoke(new WindowClosedEventArgs(this));
                IsDisposed = true;
            }
        }
    }
}
