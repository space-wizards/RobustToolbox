using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using OpenToolkit;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.Utility;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using TextureWrapMode = Robust.Shared.Graphics.TextureWrapMode;

namespace Robust.Client.Graphics.Clyde
{
    /// <summary>
    ///     Responsible for most things rendering on OpenGL mode.
    /// </summary>
    internal sealed partial class Clyde : IClydeInternal, IPostInjectInit, IEntityEventSubscriber
    {
        [Dependency] private readonly IClydeTileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly ILightManager _lightManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IResourceManager _resManager = default!;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly ProfManager _prof = default!;
        [Dependency] private readonly IDependencyCollection _deps = default!;
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly ClientEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IReloadManager _reloads = default!;

        private GLUniformBuffer<ProjViewMatrices> ProjViewUBO = default!;
        private GLUniformBuffer<UniformConstants> UniformConstantsUBO = default!;

        private GLBuffer BatchVBO = default!;
        private GLBuffer BatchEBO = default!;
        private GLHandle BatchVAO;

        // VBO to draw a single quad.
        private GLBuffer QuadVBO = default!;
        private GLHandle QuadVAO;

        // VBO to blit to the window
        // VAO is per-window and not stored (not necessary!)
        private GLBuffer WindowVBO = default!;

        private bool _drawingSplash = true;

        private GLShaderProgram? _currentProgram;

        private float _lightResolutionScale = 0.5f;
        private int _maxLights = 2048;
        private int _maxOccluders = 2048;
        private int _maxShadowcastingLights = 128;
        private bool _enableSoftShadows = true;

        private bool _checkGLErrors;

        private Thread? _gameThread;

        private ISawmill _clydeSawmill = default!;
        private ISawmill _sawmillOgl = default!;
        private ISawmill _sawmillWin = default!;

        private IBindingsContext _glBindingsContext = default!;
        private bool _earlyGLInit;
        private bool _threadWindowApi;

        public Clyde()
        {
            _currentBoundRenderTarget = default!;
            _currentRenderTarget = default!;
            SixLabors.ImageSharp.Configuration.Default.PreferContiguousImageBuffers = true;
        }

        public bool InitializePreWindowing()
        {
            _clydeSawmill = _logManager.GetSawmill("clyde");
            _sawmillOgl = _logManager.GetSawmill("clyde.ogl");
            _sawmillWin = _logManager.GetSawmill("clyde.win");

            _reloads.Register("/Shaders", "*.swsl");
            _reloads.Register("/Textures/Shaders", "*.swsl");
            _reloads.Register("/Textures", "*.jpg");
            _reloads.Register("/Textures", "*.jpeg");
            _reloads.Register("/Textures", "*.png");
            _reloads.Register("/Textures", "*.webp");

            _reloads.OnChanged += OnChange;
            _proto.PrototypesReloaded += OnProtoReload;

            _cfg.OnValueChanged(CVars.DisplayOGLCheckErrors, b => _checkGLErrors = b, true);
            _cfg.OnValueChanged(CVars.DisplayVSync, VSyncChanged, true);
            _cfg.OnValueChanged(CVars.DisplayWindowMode, WindowModeChanged, true);
            _cfg.OnValueChanged(CVars.LightResolutionScale, LightResolutionScaleChanged, true);
            _cfg.OnValueChanged(CVars.MaxShadowcastingLights, MaxShadowcastingLightsChanged, true);
            _cfg.OnValueChanged(CVars.LightSoftShadows, SoftShadowsChanged, true);
            _cfg.OnValueChanged(CVars.MaxLightCount, MaxLightsChanged, true);
            _cfg.OnValueChanged(CVars.MaxOccluderCount, MaxOccludersChanged, true);
            _cfg.OnValueChanged(CVars.RenderTileEdges, RenderTileEdgesChanges, true);
            // I can't be bothered to tear down and set these threads up in a cvar change handler.

            // Windows and Linux can be trusted to not explode with threaded windowing,
            // macOS cannot.
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
                _cfg.OverrideDefault(CVars.DisplayThreadWindowApi, true);

            _threadWindowBlit = _cfg.GetCVar(CVars.DisplayThreadWindowBlit);
            _threadWindowApi = _cfg.GetCVar(CVars.DisplayThreadWindowApi);

            InitKeys();

            return InitWindowing();
        }

        private void OnProtoReload(PrototypesReloadedEventArgs obj)
        {
            if (!obj.WasModified<ShaderPrototype>())
                return;

            foreach (var shader in obj.ByType[typeof(ShaderPrototype)].Modified.Keys)
            {
                _resourceCache.ReloadResource<ShaderSourceResource>(shader);
            }
        }

        private void OnChange(ResPath obj)
        {
            if ((obj.TryRelativeTo(new ResPath("/Shaders"), out _) || obj.TryRelativeTo(new ResPath("/Textures/Shaders"), out _)) && obj.Extension == "swsl")
            {
                _resourceCache.ReloadResource<ShaderSourceResource>(obj);
            }

            if (obj.TryRelativeTo(new ResPath("/Textures"), out _) && !obj.TryRelativeTo(new ResPath("/Textures/Tiles"), out _))
            {
                if (obj.Extension == "jpg" || obj.Extension == "jpeg" || obj.Extension == "webp")
                {
                    _resourceCache.ReloadResource<TextureResource>(obj);
                }

                if (obj.Extension == "png")
                {
                    _resourceCache.ReloadResource<TextureResource>(obj);
                }
            }
        }

        public bool InitializePostWindowing()
        {
            _gameThread = Thread.CurrentThread;

            InitSystems();

            InitGLContextManager();
            if (!InitMainWindowAndRenderer())
                return false;

            return true;
        }

        public bool SeparateWindowThread => _threadWindowApi;

        public void EnterWindowLoop()
        {
            _windowing!.EnterWindowLoop();
        }

        public void TerminateWindowLoop()
        {
            _windowing!.TerminateWindowLoop();
        }

        public void FrameProcess(FrameEventArgs eventArgs)
        {
            if (!_threadWindowApi)
            {
                _windowing!.PollEvents();
            }

            _windowing?.FlushDispose();
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

        public void PostInject()
        {
            // This cvar does not modify the actual GL version requested or anything,
            // it overrides the version we detect to detect GL features.
            RegisterBlockCVars();
        }

        public void RegisterGridEcsEvents()
        {
            _entityManager.EventBus.SubscribeEvent<TileChangedEvent>(EventSource.Local, this, _updateTileMapOnUpdate);
            _entityManager.EventBus.SubscribeEvent<GridStartupEvent>(EventSource.Local, this, _updateOnGridCreated);
            _entityManager.EventBus.SubscribeEvent<GridRemovalEvent>(EventSource.Local, this, _updateOnGridRemoved);
        }

        public void ShutdownGridEcsEvents()
        {
            _entityManager.EventBus.UnsubscribeEvent<TileChangedEvent>(EventSource.Local, this);
            _entityManager.EventBus.UnsubscribeEvent<GridStartupEvent>(EventSource.Local, this);
            _entityManager.EventBus.UnsubscribeEvent<GridRemovalEvent>(EventSource.Local, this);
        }

        private void GLInitBindings(bool gles)
        {
            _glBindingsContext = _glContext!.BindingsContext;
            GL.LoadBindings(_glBindingsContext);

            if (gles)
            {
                // On GLES we use some OES and KHR functions so make sure to initialize them.
                OpenToolkit.Graphics.ES20.GL.LoadBindings(_glBindingsContext);
            }
        }

        private void InitOpenGL()
        {
            _isGLES = _openGLVersion is RendererOpenGLVersion.GLES2 or RendererOpenGLVersion.GLES3;
            _isGLES2 = _openGLVersion is RendererOpenGLVersion.GLES2;
            _isCore = _openGLVersion is RendererOpenGLVersion.GL33;

            GLInitBindings(_isGLES);

            var vendor = GL.GetString(StringName.Vendor);
            var renderer = GL.GetString(StringName.Renderer);
            var version = GL.GetString(StringName.Version);
            // GLES2 doesn't allow you to query major/minor version. Seriously.
            var major = _openGLVersion == RendererOpenGLVersion.GLES2 ? 2 : GL.GetInteger(GetPName.MajorVersion);
            var minor = _openGLVersion == RendererOpenGLVersion.GLES2 ? 0 :GL.GetInteger(GetPName.MinorVersion);

            _sawmillOgl.Debug("OpenGL Vendor: {0}", vendor);
            _sawmillOgl.Debug("OpenGL Renderer: {0}", renderer);
            _sawmillOgl.Debug("OpenGL Version: {0}", version);

            var overrideVersion = ParseGLOverrideVersion();

            if (overrideVersion != null)
            {
                (major, minor) = overrideVersion.Value;
                _sawmillOgl.Debug("OVERRIDING detected GL version to: {0}.{1}", major, minor);
            }

            DetectOpenGLFeatures(major, minor);
            SetupDebugCallback();

            LoadVendorSettings(vendor, renderer, version);

            var glVersion = new OpenGLVersion((byte) major, (byte) minor, _isGLES, _isCore);

            DebugInfo = new ClydeDebugInfo(
                glVersion,
                renderer,
                vendor,
                version,
                overrideVersion != null,
                _windowing!.GetDescription());

            IsBlending = true;
            if (_hasGLSrgb && !_isGLES)
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
            if (_hasGLPrimitiveRestartFixedIndex)
            {
                GL.Enable(EnableCap.PrimitiveRestartFixedIndex);
                CheckGlError();
            }
            if (!HasGLAnyVertexArrayObjects)
            {
                _sawmillOgl.Warning("NO VERTEX ARRAY OBJECTS! Things will probably go terribly, terribly wrong (no fallback path yet)");
            }

            ResetBlendFunc();

            CheckGlError();

            // Primitive Restart's presence or lack thereof changes the amount of required memory.
            InitRenderingBatchBuffers();

            _sawmillOgl.Debug("Loading stock textures...");

            LoadStockTextures();

            _sawmillOgl.Debug("Loading stock shaders...");

            LoadStockShaders();

            _sawmillOgl.Debug("Creating various GL objects...");

            CreateMiscGLObjects();

            _sawmillOgl.Debug("Setting up RenderHandle...");

            _renderHandle = new RenderHandle(this, _entityManager);
        }

        private (int major, int minor)? ParseGLOverrideVersion()
        {
            var overrideGLVersion = _cfg.GetCVar(CVars.DisplayOGLOverrideVersion);
            if (string.IsNullOrEmpty(overrideGLVersion))
            {
                return null;
            }

            var split = overrideGLVersion.Split(".");
            if (split.Length != 2)
            {
                _sawmillOgl.Warning("display.ogl_override_version is in invalid format");
                return null;
            }

            if (!int.TryParse(split[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var major)
                || !int.TryParse(split[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor))
            {
                _sawmillOgl.Warning("display.ogl_override_version is in invalid format");
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
                    new Vertex2D(1, 0, 1, 1, Color.White),
                    new Vertex2D(0, 0, 0, 1, Color.White),
                    new Vertex2D(1, 1, 1, 0, Color.White),
                    new Vertex2D(0, 1, 0, 0, Color.White)
                };

                QuadVBO = new GLBuffer<Vertex2D>(this, BufferTarget.ArrayBuffer, BufferUsageHint.StaticDraw,
                    quadVertices,
                    nameof(QuadVBO));

                QuadVAO = MakeQuadVao();

                CheckGlError();
            }

            // Window VBO
            {
                Span<Vertex2D> winVertices = stackalloc[]
                {
                    new Vertex2D(-1, 1, 0, 1, Color.White),
                    new Vertex2D(-1, -1, 0, 0, Color.White),
                    new Vertex2D(1, 1, 1, 1, Color.White),
                    new Vertex2D(1, -1, 1, 0, Color.White),
                };

                WindowVBO = new GLBuffer<Vertex2D>(
                    this,
                    BufferTarget.ArrayBuffer,
                    BufferUsageHint.StaticDraw,
                    winVertices,
                    nameof(WindowVBO));

                CheckGlError();
            }

            // Batch rendering
            {
                BatchVBO = new GLBuffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                    sizeof(Vertex2D) * BatchVertexData.Length, nameof(BatchVBO));

                BatchVAO = new GLHandle(GenVertexArray());
                BindVertexArray(BatchVAO.Handle);
                ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, BatchVAO, nameof(BatchVAO));
                SetupVAOLayout();

                CheckGlError();

                BatchEBO = new GLBuffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw,
                    sizeof(ushort) * BatchIndexData.Length, nameof(BatchEBO));
            }

            ProjViewUBO = new GLUniformBuffer<ProjViewMatrices>(this, BindingIndexProjView, nameof(ProjViewUBO));
            UniformConstantsUBO = new GLUniformBuffer<UniformConstants>(this, BindingIndexUniformConstants, nameof(UniformConstantsUBO));

            screenBufferHandle = new GLHandle(GL.GenTexture());
            GL.BindTexture(TextureTarget.Texture2D, screenBufferHandle.Handle);
            ApplySampleParameters(new TextureSampleParameters() { Filter = false, WrapMode = TextureWrapMode.MirroredRepeat});
            // TODO: This is atrocious and broken and awful why did I merge this
            ScreenBufferTexture = GenTexture(screenBufferHandle, (1920, 1080), true, null, TexturePixelType.Rgba32);
        }

        private GLHandle MakeQuadVao()
        {
            var vao = new GLHandle(GenVertexArray());
            BindVertexArray(vao.Handle);
            ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, vao, nameof(QuadVAO));
            GL.BindBuffer(BufferTarget.ArrayBuffer, QuadVBO.ObjectHandle);
            SetupVAOLayout();

            return vao;
        }

        [Conditional("DEBUG")]
        private unsafe void SetupDebugCallback()
        {
            if (!_hasGLKhrDebug)
            {
                _sawmillOgl.Debug("KHR_debug not present, OpenGL debug logging not enabled.");
                return;
            }

            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);

            _debugMessageCallbackInstance ??= DebugMessageCallback;

            // OpenTK seemed to have trouble marshalling the delegate so do it manually.

            var procName = _isGLKhrDebugESExtension ? "glDebugMessageCallbackKHR" : "glDebugMessageCallback";
            var glDebugMessageCallback = (delegate* unmanaged[Stdcall] <nint, nint, void>) LoadGLProc(procName);
            var funcPtr = Marshal.GetFunctionPointerForDelegate(_debugMessageCallbackInstance);
            glDebugMessageCallback(funcPtr, new IntPtr(0x3005));
        }

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

            if (!_hasGLKhrDebug || !_glDebuggerPresent)
                return;

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
            // ANGLE spams console log messages when using debug groups, so let's only use them if we're debugging GL.
            if (!_hasGLKhrDebug || !_glDebuggerPresent)
                return;

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
            if (!_hasGLKhrDebug || !_glDebuggerPresent)
                return;

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
            _glContext?.Shutdown();
            ShutdownWindowing();
        }

        private bool IsMainThread()
        {
            return Thread.CurrentThread == _gameThread;
        }
    }
}
