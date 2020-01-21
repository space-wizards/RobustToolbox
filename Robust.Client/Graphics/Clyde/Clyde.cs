using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL;
using Robust.Client.Graphics.ClientEye;
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
using FrameEventArgs = Robust.Shared.Timing.FrameEventArgs;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Client.Graphics.Clyde
{
    /// <summary>
    ///     Responsible for most things rendering on OpenGL mode.
    /// </summary>
    internal sealed partial class Clyde : ClydeBase, IClydeInternal, IClydeAudio, IDisposable
    {
#pragma warning disable 649
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IComponentManager _componentManager;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager;
        [Dependency] private readonly IClydeTileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly ILightManager _lightManager;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        private static readonly Version MinimumOpenGLVersion = new Version(3, 3);

        //private GameWindow _window;

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

        private const int UniIModUV = 0;
        private const int UniIModelMatrix = 1;
        private const int UniIModulate = 2;
        private const int UniITexturePixelSize = 3;
        private const int UniIMainTexture = 4;
        private const int UniILightTexture = 5;
        private const int UniCount = 6;
        private const string UniModUV = "modifyUV";
        private const string UniModelMatrix = "modelMatrix";
        private const string UniModulate = "modulate";
        private const string UniTexturePixelSize = "TEXTURE_PIXEL_SIZE";
        private const string UniMainTexture = "TEXTURE";
        private const string UniLightTexture = "lightMap";

        // Thread the window is instantiated on.
        // OpenGL is allergic to multi threading so we need to check this.
        private bool _drawingSplash;

        private ShaderProgram _currentProgram;

        private ClydeDebugStats _debugStats;

        private readonly HashSet<string> OpenGLExtensions = new HashSet<string>();

        private bool HasKHRDebug => HasExtension("GL_KHR_debug");

        private bool _quartResLights = true;

        private bool _canDoStencil8RenderBuffer;

        public override bool Initialize()
        {
            _debugStats = new ClydeDebugStats();
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
            _renderTime += eventArgs.DeltaSeconds;
            _updateAudio();
            ClearDeadShaderInstances();
        }

        public void Ready()
        {
            _drawingSplash = false;
        }

        public IClydeDebugInfo DebugInfo { get; private set; }
        public IClydeDebugStats DebugStats => _debugStats;

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

        private void InitOpenGL()
        {
            _loadExtensions();

            _hijackDebugCallback();

            var vendor = GL.GetString(StringName.Vendor);
            var renderer = GL.GetString(StringName.Renderer);
            var version = GL.GetString(StringName.Version);
            Logger.DebugS("clyde.ogl", "OpenGL Vendor: {0}", vendor);
            Logger.DebugS("clyde.ogl", "OpenGL Renderer: {0}", renderer);
            Logger.DebugS("clyde.ogl", "OpenGL Version: {0}", version);
            _loadVendorSettings(vendor, renderer, version);

            DetectOpenGLFeatures();

            var major = GL.GetInteger(GetPName.MajorVersion);
            var minor = GL.GetInteger(GetPName.MinorVersion);

            DebugInfo = new ClydeDebugInfo(new Version(major, minor), MinimumOpenGLVersion, renderer, vendor, version);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.FramebufferSrgb);
            GL.Enable(EnableCap.PrimitiveRestart);
            GL.PrimitiveRestartIndex(ushort.MaxValue);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

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

            // Batch rendering
            {
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
            }

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
                RenderTargetColorFormat.Rgba8Srgb, name: nameof(EntityPostRenderTarget), hasStencilBuffer: true);

            _drawingSplash = true;

            _renderHandle = new RenderHandle(this);

            GL.Viewport(0, 0, ScreenSize.X, ScreenSize.Y);

            // Quickly do a render with _drawingSplash = true so the screen isn't blank.
                Render();
        }

        private void DetectOpenGLFeatures()
        {
            if (HasExtension("GL_ARB_ES3_compatibility"))
            {
                _canDoStencil8RenderBuffer = true;
                Logger.DebugS("clyde.ogl.ext", "Have GL_ARB_ES3_compatibility, GL_STENCIL_INDEX8 supported.");
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private void _loadVendorSettings(string vendor, string renderer, string version)
        {
            // Nothing yet.
        }

        private Vector2i _lightMapSize()
        {
            if (!_quartResLights)
            {
                return (ScreenSize.X, ScreenSize.Y);
            }

            var w = (int) Math.Ceiling(ScreenSize.X / 2f);
            var h = (int) Math.Ceiling(ScreenSize.Y / 2f);

            return (w, h);
        }

        [Conditional("DEBUG")]
        private void _hijackDebugCallback()
        {
            if (!HasKHRDebug)
            {
                Logger.DebugS("clyde.ogl", "KHR_debug not present, OpenGL debug logging not enabled.");
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
            ShutdownWindowing();
            _shutdownAudio();
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
            LightRenderTarget = CreateRenderTarget(_lightMapSize(), RenderTargetColorFormat.R11FG11FB10F,
                name: "LightRenderTarget");
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
            [FieldOffset(0 * sizeof(float))] OpenTK.Matrix4 ProjMatrix;
            [FieldOffset(16 * sizeof(float))] OpenTK.Matrix4 ViewMatrix;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ProjViewMatrices(in Matrix3 projMatrix, in Matrix3 viewMatrix)
            {
                this.ProjMatrix = new OpenTK.Matrix4(
                    projMatrix.R0C0, projMatrix.R1C0, projMatrix.R2C0, 0,
                    projMatrix.R0C1, projMatrix.R1C1, projMatrix.R2C1, 0,
                    projMatrix.R0C2, projMatrix.R1C2, projMatrix.R2C2, 0,
                    0, 0, 0, 1
                );

                this.ViewMatrix = new OpenTK.Matrix4(
                    viewMatrix.R0C0, viewMatrix.R1C0, viewMatrix.R2C0, 0,
                    viewMatrix.R0C1, viewMatrix.R1C1, viewMatrix.R2C1, 0,
                    viewMatrix.R0C2, viewMatrix.R1C2, viewMatrix.R2C2, 0,
                    0, 0, 0, 1
                );
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ProjViewMatrices(in ProjViewMatrices readProjMatrix, in Matrix3 viewMatrix)
            {
                this.ProjMatrix = readProjMatrix.ProjMatrix;

                this.ViewMatrix = new OpenTK.Matrix4(
                    viewMatrix.R0C0, viewMatrix.R1C0, viewMatrix.R2C0, 0,
                    viewMatrix.R0C1, viewMatrix.R1C1, viewMatrix.R2C1, 0,
                    viewMatrix.R0C2, viewMatrix.R1C2, viewMatrix.R2C2, 0,
                    0, 0, 0, 1
                );
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
            public ClydeDebugInfo(Version openGLVersion, Version minimumVersion, string renderer, string vendor,
                string versionString)
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

        private sealed class ClydeDebugStats : IClydeDebugStats
        {
            public int LastGLDrawCalls { get; set; }
            public int LastClydeDrawCalls { get; set; }
            public int LastBatches { get; set; }

            public void Reset()
            {
                LastGLDrawCalls = 0;
                LastClydeDrawCalls = 0;
                LastBatches = 0;
            }
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
