using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.Map;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Client.Graphics.Clyde
{
    /// <summary>
    ///     Responsible for most things rendering on OpenGL mode.
    /// </summary>
    internal sealed partial class Clyde : ClydeBase, IClydeInternal, IClydeAudio
    {
        [Dependency] private readonly IClydeTileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly ILightManager _lightManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private static readonly Version MinimumOpenGLVersion = new Version(3, 3);

        private GLBuffer ProjViewUBO = default!;
        private GLBuffer UniformConstantsUBO = default!;

        private RenderTexture EntityPostRenderTarget = default!;

        private GLBuffer BatchVBO = default!;
        private GLBuffer BatchEBO = default!;
        private GLHandle BatchVAO;

        // VBO to draw a single quad.
        private GLBuffer QuadVBO = default!;
        private GLHandle QuadVAO;

        private Viewport _mainViewport = default!;

        private bool _drawingSplash = true;

        private GLShaderProgram? _currentProgram;

        private bool _quartResLights = true;

        private bool _hasKhrDebug;

        private readonly List<(ScreenshotType type, Action<Image<Rgb24>> callback)> _queuedScreenshots
            = new List<(ScreenshotType, Action<Image<Rgb24>>)>();

        private readonly List<(uint pbo, IntPtr sync, Vector2i size, Action<Image<Rgb24>> callback)>
            _transferringScreenshots
                = new List<(uint, IntPtr, Vector2i, Action<Image<Rgb24>> )>();

        public Clyde()
        {
            var windowRid = AllocRid();
            var window = new RenderWindow(this, windowRid);

            MainWindowRenderTarget = window;
            _currentRenderTarget = window;

            _renderTargets.Add(windowRid, window);
        }

        public override bool Initialize()
        {
            if (!InitWindowing())
            {
                return false;
            }

            _initializeAudio();
            ReloadConfig();

            return true;
        }

        public void FrameProcess(FrameEventArgs eventArgs)
        {
            _updateAudio();
            FlushCursorDisposeQueue();
            ClearDeadShaderInstances();
        }

        public void Ready()
        {
            _drawingSplash = false;

            InitLighting();
        }

        public IClydeDebugInfo DebugInfo { get; private set; } = default!;
        public IClydeDebugStats DebugStats => _debugStats;

        protected override void ReadConfig()
        {
            base.ReadConfig();
            _quartResLights = !_configurationManager.GetCVar<bool>("display.highreslights");
        }

        protected override void ReloadConfig()
        {
            base.ReloadConfig();

            RegenAllLightRts();
        }

        public override void PostInject()
        {
            base.PostInject();

            _mapManager.TileChanged += _updateTileMapOnUpdate;
            _mapManager.OnGridCreated += _updateOnGridCreated;
            _mapManager.OnGridRemoved += _updateOnGridRemoved;
            _mapManager.GridChanged += _updateOnGridModified;
        }

        public override event Action<WindowResizedEventArgs>? OnWindowResized;

        public void Screenshot(ScreenshotType type, Action<Image<Rgb24>> callback)
        {
            _queuedScreenshots.Add((type, callback));
        }

        private void InitOpenGL()
        {
            DetectOpenGLFeatures();

            SetupDebugCallback();

            var vendor = GL.GetString(StringName.Vendor);
            var renderer = GL.GetString(StringName.Renderer);
            var version = GL.GetString(StringName.Version);
            Logger.DebugS("clyde.ogl", "OpenGL Vendor: {0}", vendor);
            Logger.DebugS("clyde.ogl", "OpenGL Renderer: {0}", renderer);
            Logger.DebugS("clyde.ogl", "OpenGL Version: {0}", version);

            LoadVendorSettings(vendor, renderer, version);

            var major = GL.GetInteger(GetPName.MajorVersion);
            var minor = GL.GetInteger(GetPName.MinorVersion);

            DebugInfo = new ClydeDebugInfo(new Version(major, minor), MinimumOpenGLVersion, renderer, vendor, version);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.FramebufferSrgb);
            GL.Enable(EnableCap.PrimitiveRestart);
            GL.PrimitiveRestartIndex(ushort.MaxValue);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            LoadStockTextures();
            LoadStockShaders();

            CreateMiscGLObjects();

            _renderHandle = new RenderHandle(this);

            GL.Viewport(0, 0, ScreenSize.X, ScreenSize.Y);

            // Quickly do a render with _drawingSplash = true so the screen isn't blank.
            Render();
        }

        private unsafe void CreateMiscGLObjects()
        {
            // Quad drawing.
            {
                var quadVertices = new[]
                {
                    new Vertex2D(1, 0, 1, 1),
                    new Vertex2D(0, 0, 0, 1),
                    new Vertex2D(1, 1, 1, 0),
                    new Vertex2D(0, 1, 0, 0)
                };

                QuadVBO = new GLBuffer<Vertex2D>(this, BufferTarget.ArrayBuffer, BufferUsageHint.StaticDraw,
                    quadVertices,
                    nameof(QuadVBO));

                QuadVAO = new GLHandle((uint) GL.GenVertexArray());
                GL.BindVertexArray(QuadVAO.Handle);
                ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, QuadVAO, nameof(QuadVAO));
                // Vertex Coords
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 0);
                GL.EnableVertexAttribArray(0);
                // Texture Coords.
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 2 * sizeof(float));
                GL.EnableVertexAttribArray(1);
            }

            // Batch rendering
            {
                BatchVBO = new GLBuffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                    Vertex2D.SizeOf * BatchVertexData.Length, nameof(BatchVBO));

                BatchVAO = new GLHandle(GL.GenVertexArray());
                GL.BindVertexArray(BatchVAO.Handle);
                ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, BatchVAO, nameof(BatchVAO));
                // Vertex Coords
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 0);
                GL.EnableVertexAttribArray(0);
                // Texture Coords.
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 2 * sizeof(float));
                GL.EnableVertexAttribArray(1);

                BatchEBO = new GLBuffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw,
                    sizeof(ushort) * BatchIndexData.Length, nameof(BatchEBO));
            }

            ProjViewUBO = new GLBuffer(this, BufferTarget.UniformBuffer, BufferUsageHint.StreamDraw,
                nameof(ProjViewUBO));
            ProjViewUBO.Reallocate(sizeof(ProjViewMatrices));

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, BindingIndexProjView, ProjViewUBO.ObjectHandle);

            UniformConstantsUBO = new GLBuffer(this, BufferTarget.UniformBuffer, BufferUsageHint.StreamDraw,
                nameof(UniformConstantsUBO));
            UniformConstantsUBO.Reallocate(sizeof(UniformConstants));

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, BindingIndexUniformConstants,
                UniformConstantsUBO.ObjectHandle);

            EntityPostRenderTarget = CreateRenderTarget(Vector2i.One * 8 * EyeManager.PixelsPerMeter,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                name: nameof(EntityPostRenderTarget));

            _mainViewport = CreateViewport(_framebufferSize, nameof(_mainViewport));
        }

        private void DetectOpenGLFeatures()
        {
            var extensions = GetGLExtensions();

            if (extensions.Contains("GL_KHR_debug"))
            {
                _hasKhrDebug = true;
            }
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private void LoadVendorSettings(string vendor, string renderer, string version)
        {
            // Nothing yet.
        }

        [Conditional("DEBUG")]
        private void SetupDebugCallback()
        {
            if (!_hasKhrDebug)
            {
                Logger.DebugS("clyde.ogl", "KHR_debug not present, OpenGL debug logging not enabled.");
                return;
            }

            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);

            GCHandle.Alloc(_debugMessageCallbackInstance);

            var ep = _graphicsContext.GetProcAddress("glDebugMessageCallback");
            var d = Marshal.GetDelegateForFunctionPointer<DebugMessageCallbackDelegate>(ep);
            _debugMessageCallbackInstance = DebugMessageCallback;
            var funcPtr = Marshal.GetFunctionPointerForDelegate(_debugMessageCallbackInstance);
            d(funcPtr, new IntPtr(0x3005));
        }

        private delegate void DebugMessageCallbackDelegate(IntPtr funcPtr, IntPtr userParam);

        private void DebugMessageCallback(DebugSource source, DebugType type, int id, DebugSeverity severity,
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

            var sawmill = _logManager.GetSawmill(category);

            switch (severity)
            {
                case DebugSeverity.DontCare:
                    sawmill.Info(contents);
                    break;
                case DebugSeverity.DebugSeverityNotification:
                    sawmill.Info(contents);
                    break;
                case DebugSeverity.DebugSeverityHigh:
                    sawmill.Error(contents);
                    break;
                case DebugSeverity.DebugSeverityMedium:
                    sawmill.Error(contents);
                    break;
                case DebugSeverity.DebugSeverityLow:
                    sawmill.Warning(contents);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        }

        private static DebugProc? _debugMessageCallbackInstance;

        private static HashSet<string> GetGLExtensions()
        {
            var extensions = new HashSet<string>();

            var count = GL.GetInteger(GetPName.NumExtensions);
            for (var i = 0; i < count; i++)
            {
                var extension = GL.GetString(StringNameIndexed.Extensions, i);
                extensions.Add(extension);
            }

            return extensions;
        }

        [Conditional("DEBUG")]
        private void ObjectLabelMaybe(ObjectLabelIdentifier identifier, uint name, string? label)
        {
            if (label == null)
            {
                return;
            }

            if (!_hasKhrDebug)
            {
                return;
            }

            GL.ObjectLabel(identifier, name, label.Length, label);
        }

        [Conditional("DEBUG")]
        private void ObjectLabelMaybe(ObjectLabelIdentifier identifier, GLHandle name, string? label)
        {
            ObjectLabelMaybe(identifier, name.Handle, label);
        }

        private PopDebugGroup DebugGroup(string group)
        {
            PushDebugGroupMaybe(group);
            return new PopDebugGroup(this);
        }

        [Conditional("DEBUG")]
        private void PushDebugGroupMaybe(string group)
        {
            if (!_hasKhrDebug)
            {
                return;
            }

            GL.PushDebugGroup(DebugSourceExternal.DebugSourceApplication, 0, group.Length, group);
        }

        [Conditional("DEBUG")]
        private void PopDebugGroupMaybe()
        {
            if (!_hasKhrDebug)
            {
                return;
            }

            GL.PopDebugGroup();
        }

        public void Shutdown()
        {
            ShutdownWindowing();
            _shutdownAudio();
        }
    }
}
