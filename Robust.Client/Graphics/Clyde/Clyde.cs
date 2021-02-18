using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
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
    internal sealed partial class Clyde : ClydeBase, IClydeInternal, IClydeAudio, IPostInjectInit
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

        private GLUniformBuffer<ProjViewMatrices> ProjViewUBO = default!;
        private GLUniformBuffer<UniformConstants> UniformConstantsUBO = default!;

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

        private int _lightmapDivider = 2;
        private int _maxLightsPerScene = 128;
        private bool _enableSoftShadows = true;

        private bool _checkGLErrors;

        private readonly List<(ScreenshotType type, Action<Image<Rgb24>> callback)> _queuedScreenshots
            = new();

        private readonly List<(uint pbo, IntPtr sync, Vector2i size, Action<Image<Rgb24>> callback)>
            _transferringScreenshots
                = new();

        public Clyde()
        {
            // Init main window render target.
            var windowRid = AllocRid();
            var window = new RenderWindow(this, windowRid);
            var loadedData = new LoadedRenderTarget
            {
                IsWindow = true,
                IsSrgb = true
            };
            _renderTargets.Add(windowRid, loadedData);

            _mainWindowRenderTarget = window;
            _currentRenderTarget = RtToLoaded(window);
            _currentBoundRenderTarget = _currentRenderTarget;
        }

        public override bool Initialize()
        {
            base.Initialize();
            
            _configurationManager.OnValueChanged(CVars.DisplayOGLCheckErrors, b => _checkGLErrors = b, true);

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

            FlushCursorDispose();
            FlushShaderInstanceDispose();
            FlushRenderTargetDispose();
            FlushTextureDispose();
            FlushViewportDispose();
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
            _lightmapDivider = _configurationManager.GetCVar(CVars.DisplayLightMapDivider);
            _maxLightsPerScene = _configurationManager.GetCVar(CVars.DisplayMaxLightsPerScene);
            _enableSoftShadows = _configurationManager.GetCVar(CVars.DisplaySoftShadows);
        }

        protected override void ReloadConfig()
        {
            base.ReloadConfig();

            RegenAllLightRts();
        }

        public void PostInject()
        {
            _mapManager.TileChanged += _updateTileMapOnUpdate;
            _mapManager.OnGridCreated += _updateOnGridCreated;
            _mapManager.OnGridRemoved += _updateOnGridRemoved;
            _mapManager.GridChanged += _updateOnGridModified;

            // This cvar does not modify the actual GL version requested or anything,
            // it overrides the version we detect to detect GL features.
            RegisterBlockCVars();
        }

        public override event Action<WindowResizedEventArgs>? OnWindowResized;

        public void Screenshot(ScreenshotType type, Action<Image<Rgb24>> callback)
        {
            _queuedScreenshots.Add((type, callback));
        }

        private void InitOpenGL()
        {
            var vendor = GL.GetString(StringName.Vendor);
            var renderer = GL.GetString(StringName.Renderer);
            var version = GL.GetString(StringName.Version);
            var major = GL.GetInteger(GetPName.MajorVersion);
            var minor = GL.GetInteger(GetPName.MinorVersion);

            Logger.DebugS("clyde.ogl", "OpenGL Vendor: {0}", vendor);
            Logger.DebugS("clyde.ogl", "OpenGL Renderer: {0}", renderer);
            Logger.DebugS("clyde.ogl", "OpenGL Version: {0}", version);

            var overrideVersion = ParseGLOverrideVersion();

            if (overrideVersion != null)
            {
                (major, minor) = overrideVersion.Value;
                Logger.DebugS("clyde.ogl", "OVERRIDING detected GL version to: {0}.{1}", major, minor);
            }

            DetectOpenGLFeatures(major, minor);
            SetupDebugCallback();

            LoadVendorSettings(vendor, renderer, version);

            var glVersion = new OpenGLVersion((byte) major, (byte) minor, _isGLES, _isCore);

            DebugInfo = new ClydeDebugInfo(glVersion, renderer, vendor, version, overrideVersion != null);

            GL.Enable(EnableCap.Blend);
            if (_hasGLSrgb)
            {
                GL.Enable(EnableCap.FramebufferSrgb);
                CheckGlError();
            }
            if (_hasGLPrimitiveRestart)
            {
                GL.Enable(EnableCap.PrimitiveRestart);
                CheckGlError();
                GL.PrimitiveRestartIndex(PrimitiveRestartIndex);
                CheckGlError();
            }
            if (!HasGLAnyVertexArrayObjects)
            {
                Logger.WarningS("clyde.ogl", "NO VERTEX ARRAY OBJECTS! Things will probably go terribly, terribly wrong (no fallback path yet)");
            }

            ResetBlendFunc();

            CheckGlError();

            // Primitive Restart's presence or lack thereof changes the amount of required memory.
            InitRenderingBatchBuffers();

            Logger.DebugS("clyde.ogl", "Loading stock textures...");

            LoadStockTextures();

            Logger.DebugS("clyde.ogl", "Loading stock shaders...");

            LoadStockShaders();

            Logger.DebugS("clyde.ogl", "Creating various GL objects...");

            CreateMiscGLObjects();

            Logger.DebugS("clyde.ogl", "Setting up RenderHandle...");

            _renderHandle = new RenderHandle(this);

            Logger.DebugS("clyde.ogl", "Setting viewport and rendering splash...");

            GL.Viewport(0, 0, ScreenSize.X, ScreenSize.Y);
            CheckGlError();

            // Quickly do a render with _drawingSplash = true so the screen isn't blank.
            Render();
        }

        private (int major, int minor)? ParseGLOverrideVersion()
        {
            var overrideGLVersion = _configurationManager.GetCVar(CVars.DisplayOGLOverrideVersion);
            if (string.IsNullOrEmpty(overrideGLVersion))
            {
                return null;
            }

            var split = overrideGLVersion.Split(".");
            if (split.Length != 2)
            {
                Logger.WarningS("clyde.ogl", "display.ogl_override_version is in invalid format");
                return null;
            }

            if (!int.TryParse(split[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var major)
                || !int.TryParse(split[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor))
            {
                Logger.WarningS("clyde.ogl", "display.ogl_override_version is in invalid format");
                return null;
            }

            return (major, minor);
        }

        private unsafe void CreateMiscGLObjects()
        {
            // Quad drawing.
            {
                Span<Vertex2D> quadVertices = stackalloc[]
                {
                    new Vertex2D(1, 0, 1, 1),
                    new Vertex2D(0, 0, 0, 1),
                    new Vertex2D(1, 1, 1, 0),
                    new Vertex2D(0, 1, 0, 0)
                };

                QuadVBO = new GLBuffer<Vertex2D>(this, BufferTarget.ArrayBuffer, BufferUsageHint.StaticDraw,
                    quadVertices,
                    nameof(QuadVBO));

                QuadVAO = new GLHandle(GenVertexArray());
                BindVertexArray(QuadVAO.Handle);
                ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, QuadVAO, nameof(QuadVAO));
                // Vertex Coords
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 0);
                GL.EnableVertexAttribArray(0);
                // Texture Coords.
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 2 * sizeof(float));
                GL.EnableVertexAttribArray(1);

                CheckGlError();
            }

            // Batch rendering
            {
                BatchVBO = new GLBuffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                    Vertex2D.SizeOf * BatchVertexData.Length, nameof(BatchVBO));

                BatchVAO = new GLHandle(GenVertexArray());
                BindVertexArray(BatchVAO.Handle);
                ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, BatchVAO, nameof(BatchVAO));
                // Vertex Coords
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 0);
                GL.EnableVertexAttribArray(0);
                // Texture Coords.
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 2 * sizeof(float));
                GL.EnableVertexAttribArray(1);

                CheckGlError();

                BatchEBO = new GLBuffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw,
                    sizeof(ushort) * BatchIndexData.Length, nameof(BatchEBO));
            }

            ProjViewUBO = new GLUniformBuffer<ProjViewMatrices>(this, BindingIndexProjView, nameof(ProjViewUBO));
            UniformConstantsUBO = new GLUniformBuffer<UniformConstants>(this, BindingIndexUniformConstants, nameof(UniformConstantsUBO));

            EntityPostRenderTarget = CreateRenderTarget(Vector2i.One * 8 * EyeManager.PixelsPerMeter,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                name: nameof(EntityPostRenderTarget));

            CreateMainViewport();

            screenBufferHandle = new GLHandle(GL.GenTexture());
            GL.BindTexture(TextureTarget.Texture2D, screenBufferHandle.Handle);
            ApplySampleParameters(TextureSampleParameters.Default);
            ScreenBufferTexture = GenTexture(screenBufferHandle, _framebufferSize, true, null, TexturePixelType.Rgba32);
        }

        private void CreateMainViewport()
        {
            var (w, h) = _framebufferSize;

            // Ensure viewport size is always even to avoid artifacts.
            if (w % 2 == 1) w += 1;
            if (h % 2 == 1) h += 1;

            _mainViewport = CreateViewport((w, h), nameof(_mainViewport));
        }

        [Conditional("DEBUG")]
        private void SetupDebugCallback()
        {
            if (!_hasGLKhrDebug)
            {
                Logger.DebugS("clyde.ogl", "KHR_debug not present, OpenGL debug logging not enabled.");
                return;
            }

            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);

            GCHandle.Alloc(_debugMessageCallbackInstance);

            // OpenTK seemed to have trouble marshalling the delegate so do it manually.

            var procName = _isGLKhrDebugESExtension ? "glDebugMessageCallbackKHR" : "glDebugMessageCallback";
            LoadGLProc(procName, out DebugMessageCallbackDelegate proc);
            _debugMessageCallbackInstance = DebugMessageCallback;
            var funcPtr = Marshal.GetFunctionPointerForDelegate(_debugMessageCallbackInstance);
            proc(funcPtr, new IntPtr(0x3005));
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

        [Conditional("DEBUG")]
        private void ObjectLabelMaybe(ObjectLabelIdentifier identifier, uint name, string? label)
        {
            if (label == null)
            {
                return;
            }

            if (!_hasGLKhrDebug)
            {
                return;
            }

            if (_isGLKhrDebugESExtension)
            {
                GL.Khr.ObjectLabel((ObjectIdentifier) identifier, name, label.Length, label);
            }
            else
            {
                GL.ObjectLabel(identifier, name, label.Length, label);
            }
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
            if (!_hasGLKhrDebug)
            {
                return;
            }

            if (_isGLKhrDebugESExtension)
            {
                GL.Khr.PushDebugGroup((DebugSource) DebugSourceExternal.DebugSourceApplication, 0, group.Length, group);
            }
            else
            {
                GL.PushDebugGroup(DebugSourceExternal.DebugSourceApplication, 0, group.Length, group);
            }
        }

        [Conditional("DEBUG")]
        private void PopDebugGroupMaybe()
        {
            if (!_hasGLKhrDebug)
            {
                return;
            }

            if (_isGLKhrDebugESExtension)
            {
                GL.Khr.PopDebugGroup();
            }
            else
            {
                GL.PopDebugGroup();
            }
        }

        public void Shutdown()
        {
            ShutdownWindowing();
            _shutdownAudio();
        }
    }
}
