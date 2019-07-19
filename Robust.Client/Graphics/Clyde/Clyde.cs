using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using JetBrains.Annotations;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Input;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.Map;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Matrix3 = Robust.Shared.Maths.Matrix3;
using Vector2 = Robust.Shared.Maths.Vector2;
using Vector3 = Robust.Shared.Maths.Vector3;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Client.Graphics.Clyde
{
    /// <summary>
    ///     Responsible for most things rendering on OpenGL mode.
    /// </summary>
    internal sealed partial class Clyde : DisplayManager, IClydeInternal, IClydeAudio, IDisposable
    {
#pragma warning disable 649
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IComponentManager _componentManager;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager;
        [Dependency] private readonly IClydeTileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly ILightManager _lightManager;
#pragma warning restore 649

        private static readonly Version MinimumOpenGLVersion = new Version(3, 3);

        private Vector2i _windowSize;
        private GameWindow _window;

        private const int ProjViewBindingIndex = 0;
        private const int UniformConstantsBindingIndex = 1;
        private Buffer ProjViewUBO;
        private Buffer UniformConstantsUBO;

        private RenderTarget LightRenderTarget;
        private RenderTarget EntityPostRenderTarget;

        private Buffer BatchVBO;
        private Buffer BatchEBO;
        private OGLHandle BatchVAO;

        // VBO to draw a single quad.
        private Buffer QuadVBO;

        private OGLHandle QuadVAO;

        // VBO to draw a single line.
        private Buffer LineVBO;
        private OGLHandle LineVAO;

        private const string UniModUV = "modifyUV";
        private const string UniModelMatrix = "modelMatrix";
        private const string UniModulate = "modulate";
        private const string UniTexturePixelSize = "TEXTURE_PIXEL_SIZE";
        private const string UniMainTexture = "TEXTURE";
        private const string UniLightTexture = "lightMap";

        // Thread the window is instantiated on.
        // OpenGL is allergic to multi threading so we need to check this.
        private Thread _mainThread;
        private bool _drawingSplash;

        private ShaderProgram _currentProgram;

        public override Vector2i ScreenSize => new Vector2i(_window.Width, _window.Height);
        private readonly HashSet<string> OpenGLExtensions = new HashSet<string>();

        private bool HasKHRDebug => HasExtension("GL_KHR_debug");

        private bool _quartResLights = true;

        private bool _disposing;

        public override void SetWindowTitle(string title)
        {
            _window.Title = title;
        }

        public override void Initialize()
        {
            _initWindow();
            _initializeAudio();
            ReloadConfig();
        }

        public void FrameProcess(RenderFrameEventArgs eventArgs)
        {
            _renderTime += eventArgs.Elapsed;
            _updateAudio();
        }

        public void ProcessInput(FrameEventArgs frameEventArgs)
        {
            _window.ProcessEvents();
        }

        public void Ready()
        {
            _drawingSplash = false;
        }

        public Vector2 MouseScreenPosition { get; private set; }
        public IClydeDebugInfo DebugInfo { get; private set; }

        protected override void ReadConfig()
        {
            base.ReadConfig();
            _quartResLights = !_configurationManager.GetCVar<bool>("display.highreslights");
        }

        protected override void ReloadConfig()
        {
            base.ReloadConfig();

            _regenerateLightRenderTarget();
        }

        public override void PostInject()
        {
            base.PostInject();

            _mapManager.TileChanged += _updateTileMapOnUpdate;
            _mapManager.OnGridCreated += _updateOnGridCreated;
            _mapManager.OnGridRemoved += _updateOnGridRemoved;
            _mapManager.GridChanged += _updateOnGridModified;
        }

        public override event Action<WindowResizedEventArgs> OnWindowResized;

        private void _initWindow()
        {
            var width = _configurationManager.GetCVar<int>("display.width");
            var height = _configurationManager.GetCVar<int>("display.height");

            _window = new GameWindow(
                width,
                height,
                GraphicsMode.Default,
                "Space Station 14",
                GameWindowFlags.Default,
                DisplayDevice.Default,
                3, 3,
#if DEBUG
                GraphicsContextFlags.Debug | GraphicsContextFlags.ForwardCompatible
#else
                GraphicsContextFlags.ForwardCompatible
#endif
            )
            {
                Visible = true
            };

            // Actually set VSync.
            VSyncChanged();

            _windowSize = new Vector2i(_window.Width, _window.Height);

            _mainThread = Thread.CurrentThread;

            _window.KeyDown += (sender, eventArgs) => { _gameController.KeyDown((KeyEventArgs) eventArgs); };

            _window.KeyUp += (sender, eventArgs) => { _gameController.KeyUp((KeyEventArgs) eventArgs); };
            _window.Closed += _onWindowClosed;
            _window.Resize += (sender, eventArgs) =>
            {
                var oldSize = _windowSize;
                _windowSize = new Vector2i(_window.Width, _window.Height);
                GL.Viewport(0, 0, _window.Width, _window.Height);
                _regenerateLightRenderTarget();
                OnWindowResized?.Invoke(new WindowResizedEventArgs(oldSize, _windowSize));
            };
            _window.MouseDown += (sender, eventArgs) =>
            {
                var mouseButtonEventArgs = (MouseButtonEventArgs) eventArgs;
                _gameController.MouseDown(mouseButtonEventArgs);
                if (!mouseButtonEventArgs.Handled)
                {
                    _gameController.KeyDown((KeyEventArgs) eventArgs);
                }
            };
            _window.MouseUp += (sender, eventArgs) =>
            {
                var mouseButtonEventArgs = (MouseButtonEventArgs) eventArgs;
                _gameController.MouseUp(mouseButtonEventArgs);
                if (!mouseButtonEventArgs.Handled)
                {
                    _gameController.KeyUp((KeyEventArgs) eventArgs);
                }
            };
            _window.MouseMove += (sender, eventArgs) =>
            {
                MouseScreenPosition = new Vector2(eventArgs.X, eventArgs.Y);
                _gameController.MouseMove((MouseMoveEventArgs) eventArgs);
            };
            _window.MouseWheel += (sender, eventArgs) =>
            {
                _gameController.MouseWheel((MouseWheelEventArgs) eventArgs);
            };
            _window.KeyPress += (sender, eventArgs) =>
            {
                // If this is a surrogate it has to be specifically handled and I'm not doing that yet.
                DebugTools.Assert(!char.IsSurrogate(eventArgs.KeyChar));

                _gameController.TextEntered(new TextEventArgs(eventArgs.KeyChar));
            };

            using (var iconFile = _resourceCache.ContentFileRead("/Textures/Logo/icon.ico"))
            {
                _window.Icon = new Icon(iconFile);
            }

            _initOpenGL();
        }

        private void _initOpenGL()
        {
            _loadExtensions();

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.FramebufferSrgb);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            var vendor = GL.GetString(StringName.Vendor);
            var renderer = GL.GetString(StringName.Renderer);
            var version = GL.GetString(StringName.Version);
            Logger.DebugS("ogl", "OpenGL Vendor: {0}", vendor);
            Logger.DebugS("ogl", "OpenGL Renderer: {0}", renderer);
            Logger.DebugS("ogl", "OpenGL Version: {0}", version);
            _loadVendorSettings(vendor, renderer, version);

            var major = GL.GetInteger(GetPName.MajorVersion);
            var minor = GL.GetInteger(GetPName.MinorVersion);

            DebugInfo = new ClydeDebugInfo(new Version(major, minor), MinimumOpenGLVersion, renderer, vendor, version);

#if DEBUG
            _hijackDebugCallback();
#endif

            _loadStockTextures();
            _loadStockShaders();

            // Quad drawing.
            {
                var quadVertices = new[]
                {
                    new Vertex2D(1, 0, 1, 1),
                    new Vertex2D(0, 0, 0, 1),
                    new Vertex2D(1, 1, 1, 0),
                    new Vertex2D(0, 1, 0, 0),
                };

                QuadVBO = new Buffer<Vertex2D>(this, BufferTarget.ArrayBuffer, BufferUsageHint.StaticDraw, quadVertices,
                    nameof(QuadVBO));

                QuadVAO = new OGLHandle((uint) GL.GenVertexArray());
                GL.BindVertexArray(QuadVAO.Handle);
                _objectLabelMaybe(ObjectLabelIdentifier.VertexArray, QuadVAO, "QuadVAO");
                // Vertex Coords
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 0);
                GL.EnableVertexAttribArray(0);
                // Texture Coords.
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 2 * sizeof(float));
                GL.EnableVertexAttribArray(1);
            }

            // Line drawing.
            {
                var lineVertices = new[]
                {
                    new Vertex2D(0, 0, 0, 0),
                    new Vertex2D(1, 1, 1, 1),
                };

                LineVBO = new Buffer<Vertex2D>(this, BufferTarget.ArrayBuffer, BufferUsageHint.StaticDraw, lineVertices,
                    nameof(LineVBO));

                LineVAO = new OGLHandle((uint) GL.GenVertexArray());
                GL.BindVertexArray(LineVAO.Handle);
                _objectLabelMaybe(ObjectLabelIdentifier.VertexArray, LineVAO, nameof(LineVAO));
                // Vertex Coords
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 0);
                GL.EnableVertexAttribArray(0);
                // Texture Coords.
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 2 * sizeof(float));
                GL.EnableVertexAttribArray(1);
            }

            BatchVBO = new Buffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                Vertex2D.SizeOf * BatchVertexData.Length, "BatchVBO");

            BatchVAO = new OGLHandle(GL.GenVertexArray());
            GL.BindVertexArray(BatchVAO.Handle);
            _objectLabelMaybe(ObjectLabelIdentifier.VertexArray, BatchVAO, "BatchVAO");
            // Vertex Coords
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 0);
            GL.EnableVertexAttribArray(0);
            // Texture Coords.
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            BatchEBO = new Buffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw,
                sizeof(ushort) * BatchIndexData.Length, "BatchEBO");

            ProjViewUBO = new Buffer(this, BufferTarget.UniformBuffer, BufferUsageHint.StreamDraw, "ProjViewUBO");
            unsafe
            {
                ProjViewUBO.Reallocate(sizeof(ProjViewMatrices));
            }

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, ProjViewBindingIndex, ProjViewUBO.ObjectHandle);

            UniformConstantsUBO = new Buffer(this, BufferTarget.UniformBuffer, BufferUsageHint.StreamDraw,
                nameof(UniformConstantsUBO));
            unsafe
            {
                UniformConstantsUBO.Reallocate(sizeof(UniformConstants));
            }

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, UniformConstantsBindingIndex,
                UniformConstantsUBO.ObjectHandle);

            _regenerateLightRenderTarget();

            EntityPostRenderTarget = CreateRenderTarget(Vector2i.One * 4 * EyeManager.PIXELSPERMETER,
                RenderTargetColorFormat.Rgba8Srgb, name: nameof(EntityPostRenderTarget));

            _drawingSplash = true;

            _renderHandle = new RenderHandle(this);

            GL.Viewport(0, 0, _window.Width, _window.Height);

            // Quickly do a render with _drawingSplash = true so the screen isn't blank.
            Render();
        }

        // ReSharper disable once UnusedParameter.Local
        private void _loadVendorSettings(string vendor, string renderer, string version)
        {
            if (vendor.IndexOf("intel", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                // Intel specific settings.
                _reallocateBuffers = true;
            }
        }

        private Vector2i _lightMapSize()
        {
            if (!_quartResLights)
            {
                return (_window.Width, _window.Height);
            }

            var w = (int) Math.Ceiling(_window.Width / 2f);
            var h = (int) Math.Ceiling(_window.Height / 2f);

            return (w, h);
        }

        private void _hijackDebugCallback()
        {
            if (!HasKHRDebug)
            {
                Logger.DebugS("ogl", "KHR_debug not present, OpenGL debug logging not enabled.");
                return;
            }

            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            GCHandle.Alloc(_debugMessageCallbackInstance);
            // See https://github.com/opentk/opentk/issues/880
            var type = typeof(GL);
            // ReSharper disable once PossibleNullReferenceException
            var entryPoints = (IntPtr[]) type.GetField("EntryPoints", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
            var ep = entryPoints[184];
            var d = Marshal.GetDelegateForFunctionPointer<DebugMessageCallbackDelegate>(ep);
            d(_debugMessageCallbackInstance, new IntPtr(0x3005));
        }

        private delegate void DebugMessageCallbackDelegate([MarshalAs(UnmanagedType.FunctionPtr)] DebugProc proc,
            IntPtr userParam);

        private static void _debugMessageCallback(DebugSource source, DebugType type, int id, DebugSeverity severity,
            int length, IntPtr message, IntPtr userParam)
        {
            var contents = $"{source}: " + Marshal.PtrToStringAnsi(message, length);

            var category = "ogl.debug";
            switch (type)
            {
                case DebugType.DebugTypePerformance:
                    category += ".performance";
                    break;
                case DebugType.DebugTypeOther:
                    category += ".other";
                    break;
                case DebugType.DebugTypeError:
                    category += ".error";
                    break;
                case DebugType.DebugTypeDeprecatedBehavior:
                    category += ".deprecated";
                    break;
                case DebugType.DebugTypeUndefinedBehavior:
                    category += ".ub";
                    break;
                case DebugType.DebugTypePortability:
                    category += ".portability";
                    break;
                case DebugType.DebugTypeMarker:
                case DebugType.DebugTypePushGroup:
                case DebugType.DebugTypePopGroup:
                    // These are inserted by our own code so I imagine they're not necessary to log?
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            switch (severity)
            {
                case DebugSeverity.DontCare:
                    Logger.InfoS(category, contents);
                    break;
                case DebugSeverity.DebugSeverityNotification:
                    Logger.InfoS(category, contents);
                    break;
                case DebugSeverity.DebugSeverityHigh:
                    Logger.ErrorS(category, contents);
                    break;
                case DebugSeverity.DebugSeverityMedium:
                    Logger.ErrorS(category, contents);
                    break;
                case DebugSeverity.DebugSeverityLow:
                    Logger.WarningS(category, contents);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        }

        private static readonly DebugProc _debugMessageCallbackInstance = _debugMessageCallback;

        private void _loadExtensions()
        {
            var count = GL.GetInteger(GetPName.NumExtensions);
            for (var i = 0; i < count; i++)
            {
                var extension = GL.GetString(StringNameIndexed.Extensions, i);
                OpenGLExtensions.Add(extension);
            }
        }

        private bool HasExtension(string extensionName)
        {
            return OpenGLExtensions.Contains(extensionName);
        }

        [Conditional("DEBUG")]
        private void _objectLabelMaybe(ObjectLabelIdentifier identifier, uint name, string label)
        {
            DebugTools.Assert(label != null);

            if (!HasKHRDebug)
            {
                return;
            }

            GL.ObjectLabel(identifier, name, label.Length, label);
        }

        [Conditional("DEBUG")]
        private void _objectLabelMaybe(ObjectLabelIdentifier identifier, OGLHandle name, string label)
        {
            _objectLabelMaybe(identifier, name.Handle, label);
        }

        private PopDebugGroup DebugGroup(string group)
        {
            _pushDebugGroupMaybe(group);
            return new PopDebugGroup(this);
        }

        [Conditional("DEBUG")]
        private void _pushDebugGroupMaybe(string group)
        {
            if (!HasKHRDebug)
            {
                return;
            }

            GL.PushDebugGroup(DebugSourceExternal.DebugSourceApplication, 0, group.Length, group);
        }

        [Conditional("DEBUG")]
        private void _popDebugGroupMaybe()
        {
            if (!HasKHRDebug)
            {
                return;
            }

            GL.PopDebugGroup();
        }

        public void Dispose()
        {
            _disposing = true;
            _window.Dispose();
            _shutdownAudio();
        }

        public IntPtr GetNativeWindowHandle()
        {
            return _window.WindowInfo.Handle;
        }

        protected override void VSyncChanged()
        {
            if (_window == null)
            {
                return;
            }

            _window.VSync = VSync ? VSyncMode.On : VSyncMode.Off;
        }

        protected override void WindowModeChanged()
        {
            if (_window == null)
            {
                return;
            }

            _window.WindowState = WindowMode == WindowMode.Fullscreen ? WindowState.Fullscreen : WindowState.Normal;
        }

        protected override void HighResLightsChanged(bool newValue)
        {
            _quartResLights = !newValue;
            if (LightRenderTarget == null)
            {
                return;
            }

            _regenerateLightRenderTarget();
        }

        private void _regenerateLightRenderTarget()
        {
            LightRenderTarget = CreateRenderTarget(_lightMapSize(), RenderTargetColorFormat.Rgba16F,
                name: "LightRenderTarget");
        }

        private void _onWindowClosed(object sender, EventArgs args)
        {
            if (_disposing)
            {
                // OpenTK seems to fire Closed when closing the window via Dispose on Windows.
                // So uh, ignore it.
                return;
            }

            _gameController.Shutdown("Window closed");
        }

        [StructLayout(LayoutKind.Sequential)]
        [PublicAPI]
        private readonly struct Vertex2D
        {
            public static readonly int SizeOf;

            public readonly Vector2 Position;
            public readonly Vector2 TextureCoordinates;

            static Vertex2D()
            {
                unsafe
                {
                    SizeOf = sizeof(Vertex2D);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex2D(Vector2 position, Vector2 textureCoordinates)
            {
                Position = position;
                TextureCoordinates = textureCoordinates;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex2D(float x, float y, float u, float v)
                : this(new Vector2(x, y), new Vector2(u, v))
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex2D(Vector2 position, float u, float v)
                : this(position, new Vector2(u, v))
            {
            }

            public override string ToString()
            {
                return $"Vertex2D: {Position}, {TextureCoordinates}";
            }
        }

        // Go through the commit log if you wanna find why this struct exists.
        // And why there's no implicit operator.
        /// <summary>
        ///     Basically just a handle around the integer object handles returned by OpenGL.
        /// </summary>
        [PublicAPI]
        private struct OGLHandle
        {
            public readonly uint Handle;

            public OGLHandle(int handle) : this((uint) handle)
            {
            }

            public OGLHandle(uint handle)
            {
                Handle = handle;
            }

            public bool Equals(OGLHandle other)
            {
                return Handle == other.Handle;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is OGLHandle other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Handle.GetHashCode();
            }

            public override string ToString()
            {
                return $"{nameof(Handle)}: {Handle}";
            }

            public static bool operator ==(OGLHandle a, OGLHandle b)
            {
                return a.Handle == b.Handle;
            }

            public static bool operator !=(OGLHandle a, OGLHandle b)
            {
                return a.Handle != b.Handle;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 28 * sizeof(float))]
        [PublicAPI]
        private struct ProjViewMatrices
        {
            [FieldOffset(0 * sizeof(float))] public Vector3 ProjMatrixC0;
            [FieldOffset(4 * sizeof(float))] public Vector3 ProjMatrixC1;
            [FieldOffset(8 * sizeof(float))] public Vector3 ProjMatrixC2;

            [FieldOffset(12 * sizeof(float))] public Vector3 ViewMatrixC0;
            [FieldOffset(16 * sizeof(float))] public Vector3 ViewMatrixC1;
            [FieldOffset(20 * sizeof(float))] public Vector3 ViewMatrixC2;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ProjViewMatrices(in Matrix3 projMatrix, in Matrix3 viewMatrix)
            {
                ProjMatrixC0 = new Vector3(projMatrix.R0C0, projMatrix.R1C0, projMatrix.R2C0);
                ProjMatrixC1 = new Vector3(projMatrix.R0C1, projMatrix.R1C1, projMatrix.R2C1);
                ProjMatrixC2 = new Vector3(projMatrix.R0C2, projMatrix.R1C2, projMatrix.R2C2);

                ViewMatrixC0 = new Vector3(viewMatrix.R0C0, viewMatrix.R1C0, viewMatrix.R2C0);
                ViewMatrixC1 = new Vector3(viewMatrix.R0C1, viewMatrix.R1C1, viewMatrix.R2C1);
                ViewMatrixC2 = new Vector3(viewMatrix.R0C2, viewMatrix.R1C2, viewMatrix.R2C2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ProjViewMatrices(in ProjViewMatrices readProjMatrix, in Matrix3 viewMatrix)
            {
                ProjMatrixC0 = readProjMatrix.ProjMatrixC0;
                ProjMatrixC1 = readProjMatrix.ProjMatrixC1;
                ProjMatrixC2 = readProjMatrix.ProjMatrixC2;

                ViewMatrixC0 = new Vector3(viewMatrix.R0C0, viewMatrix.R1C0, viewMatrix.R2C0);
                ViewMatrixC1 = new Vector3(viewMatrix.R0C1, viewMatrix.R1C1, viewMatrix.R2C1);
                ViewMatrixC2 = new Vector3(viewMatrix.R0C2, viewMatrix.R1C2, viewMatrix.R2C2);
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = sizeof(float) * 4)]
        [PublicAPI]
        private struct UniformConstants
        {
            [FieldOffset(0)] public Vector2 ScreenPixelSize;
            [FieldOffset(2 * sizeof(float))] public float Time;

            public UniformConstants(Vector2 screenPixelSize, float time)
            {
                ScreenPixelSize = screenPixelSize;
                Time = time;
            }
        }

        private sealed class ClydeDebugInfo : IClydeDebugInfo
        {
            public ClydeDebugInfo(Version openGLVersion, Version minimumVersion, string renderer, string vendor, string versionString)
            {
                OpenGLVersion = openGLVersion;
                MinimumVersion = minimumVersion;
                Renderer = renderer;
                Vendor = vendor;
                VersionString = versionString;
            }

            public Version OpenGLVersion { get; }
            public Version MinimumVersion { get; }
            public string Renderer { get; }
            public string Vendor { get; }
            public string VersionString { get; }
        }
    }

    [Serializable]
    [PublicAPI]
    internal class ShaderCompilationException : Exception
    {
        public ShaderCompilationException()
        {
        }

        public ShaderCompilationException(string message) : base(message)
        {
        }

        public ShaderCompilationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ShaderCompilationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
