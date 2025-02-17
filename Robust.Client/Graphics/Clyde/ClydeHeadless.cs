using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Robust.Client.Graphics.Clyde
{
    /// <summary>
    ///     Hey look, it's Clyde's evil twin brother!
    /// </summary>
    [UsedImplicitly]
    internal sealed class ClydeHeadless : IClydeInternal
    {
        // Would it make sense to report a fake resolution like 720p here so code doesn't break? idk.
        public IClydeWindow MainWindow { get; }
        public Vector2i ScreenSize => (1280, 720);
        public IEnumerable<IClydeWindow> AllWindows => _windows;
        public Vector2 DefaultWindowScale => new Vector2(1, 1);
        public bool IsFocused => true;
        private readonly List<IClydeWindow> _windows = new();
        private int _nextWindowId = 2;

        public ShaderInstance InstanceShader(ShaderSourceResource handle, bool? light = null, ShaderBlendMode? blend = null)
        {
            return new DummyShaderInstance();
        }

        public ClydeHeadless()
        {
            SixLabors.ImageSharp.Configuration.Default.PreferContiguousImageBuffers = true;

            var mainRt = new DummyRenderWindow(this);
            var window = new DummyWindow(mainRt) {Id = new WindowId(1)};

            _windows.Add(window);
            MainWindow = window;
        }

        public ScreenCoordinates MouseScreenPosition => default;
        public IClydeDebugInfo DebugInfo { get; } = new DummyDebugInfo();
        public IClydeDebugStats DebugStats { get; } = new DummyDebugStats();

        public event Action<TextEnteredEventArgs>? TextEntered { add { } remove { } }
        public event Action<TextEditingEventArgs>? TextEditing { add { } remove { } }
        public event Action<MouseMoveEventArgs>? MouseMove { add { } remove { } }
        public event Action<MouseEnterLeaveEventArgs>? MouseEnterLeave { add { } remove { } }
        public event Action<KeyEventArgs>? KeyUp { add { } remove { } }
        public event Action<KeyEventArgs>? KeyDown { add { } remove { } }
        public event Action<MouseWheelEventArgs>? MouseWheel { add { } remove { } }
        public event Action<WindowRequestClosedEventArgs>? CloseWindow { add { } remove { } }
        public event Action<WindowDestroyedEventArgs>? DestroyWindow { add { } remove { } }

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

        public void RegisterGridEcsEvents()
        {
            // Nada.
        }

        public void ShutdownGridEcsEvents()
        {

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

        public event Action<WindowContentScaleEventArgs> OnWindowScaleChanged
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

        /// <inheritdoc />
        public Color GetClearColor(EntityUid mapUid)
        {
            return Color.Transparent;
        }

        public void BlurRenderTarget(IClydeViewport viewport, IRenderTarget target, IRenderTarget blurBuffer, IEye eye, float multiplier)
        {
            // NOOP
        }

        public IRenderTexture CreateLightRenderTarget(Vector2i size, string? name = null, bool depthStencil = true)
        {
            return CreateRenderTarget(size, new RenderTargetFormatParameters(RenderTargetColorFormat.R8, hasDepthStencil: depthStencil), null, name: name);
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

        public IClydeWindow CreateWindow(WindowCreateParameters parameters)
        {
            var window = new DummyWindow(CreateRenderTarget((123, 123), default))
            {
                Id = new WindowId(_nextWindowId++)
            };
            _windows.Add(window);

            return window;
        }

        public void TextInputSetRect(UIBox2i rect)
        {
            // Nada.
        }

        public void TextInputStart()
        {
            // Nada.
        }

        public void TextInputStop()
        {
            // Nada.
        }

        public ClydeHandle LoadShader(ParsedShader shader, string? name = null, Dictionary<string,string>? defines = null)
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

        public Task<string> GetText()
        {
            return Task.FromResult(string.Empty);
        }

        public void SetText(string text)
        {
            // Nada.
        }

        public void RunOnWindowThread(Action action)
        {
            action();
        }

        public IFileDialogManager? FileDialogImpl => null;

        private sealed class DummyCursor : ICursor
        {
            public void Dispose()
            {
                // Nada.
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

            public override void SetSubImage<T>(Vector2i topLeft, Vector2i size, ReadOnlySpan<T> buffer)
            {
                // Just do nothing on mutate.
            }

            public override Color GetPixel(int x, int y)
            {
                return Color.Black;
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

            private protected override void SetParameterImpl(string name, float[] value)
            {
            }

            private protected override void SetParameterImpl(string name, Vector2 value)
            {
            }

            private protected override void SetParameterImpl(string name, Vector2[] value)
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

            private protected override void SetParameterImpl(string name, Color[] value)
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

            private protected override void SetParameterImpl(string name, bool[] value)
            {
            }

            private protected override void SetParameterImpl(string name, in Matrix3x2 value)
            {
            }

            private protected override void SetParameterImpl(string name, in Matrix4 value)
            {
            }

            private protected override void SetParameterImpl(string name, Texture value)
            {
            }

            private protected override void SetStencilImpl(StencilParameters value)
            {
            }

            public override void Dispose()
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
            public int ShadowLights => 0;
            public int Occluders => 0;
            public int Entities => 0;
        }

        private sealed class DummyDebugInfo : IClydeDebugInfo
        {
            public OpenGLVersion OpenGLVersion { get; } = new(3, 3, isES: false, isCore: true);
            public string Renderer => "ClydeHeadless";
            public string Vendor => "Space Wizards Federation";
            public string VersionString { get; } = $"3.3.0 WIZARDS {typeof(DummyDebugInfo).Assembly.GetName().Version}";
            public bool Overriding => false;
            public string WindowingApi => "The vast abyss";
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

            public IRenderTexture LightRenderTarget { get; } =
                new DummyRenderTexture(Vector2i.One, new DummyTexture(Vector2i.One));

            public IEye? Eye { get; set; }
            public Vector2i Size { get; }
            public Color? ClearColor { get; set; } = Color.Black;
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

            public Matrix3x2 GetWorldToLocalMatrix() => default;

            public Vector2 WorldToLocal(Vector2 point)
            {
                return default;
            }

            public void RenderScreenOverlaysBelow(
                IRenderHandle handle,
                IViewportControl control,
                in UIBox2i viewportBounds)
            {
                // Nada
            }

            public void RenderScreenOverlaysAbove(
                IRenderHandle handle,
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

            public Vector2i Size { get; set; } = default;
            public bool IsDisposed { get; private set; }
            public WindowId Id { get; set; }
            public IRenderTarget RenderTarget { get; }
            public string Title { get; set; } = "";
            public bool IsFocused => false;
            public bool IsMinimized => false;
            public bool IsVisible { get; set; } = true;
            public Vector2 ContentScale => Vector2.One;
            public bool DisposeOnClose { get; set; }
            public event Action<WindowRequestClosedEventArgs>? RequestClosed { add { } remove { } }
            public event Action<WindowDestroyedEventArgs>? Destroyed;
            public event Action<WindowResizedEventArgs>? Resized { add { } remove { } }

            public void TextInputSetRect(UIBox2i rect, int cursor)
            {
                // Nop.
            }

            public void TextInputStart()
            {
                // Nop.
            }

            public void TextInputStop()
            {
                // Nop.
            }

            public void MaximizeOnMonitor(IClydeMonitor monitor)
            {
            }

            public void Dispose()
            {
                IsDisposed = true;

                Destroyed?.Invoke(new WindowDestroyedEventArgs(this));
            }
        }
    }
}
