using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Graphics.Overlays;
using SS14.Client.Input;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using Box2 = SS14.Shared.Maths.Box2;
using Matrix3 = SS14.Shared.Maths.Matrix3;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Graphics
{
    /// <summary>
    ///     Responsible for most things rendering on OpenGL mode.
    /// </summary>
    internal sealed partial class DisplayManagerOpenGL : DisplayManager, IDisplayManagerOpenGL, IDisposable
    {
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager;

        private OpenTK.GameWindow _window;

        private int AnotherVBO;
        private int AnotherEBO;
        // VBO to draw a single quad.
        private int QuadVBO;
        private int Vertex2DProgram;
        // Locations of a few uniforms in the above program.
        private int Vertex2DUniformModel;
        private int Vertex2DUniformView;
        private int Vertex2DUniformModUV;
        private int Vertex2DUniformProjection;
        private int Vertex2DUniformModulate;
        // The main VAO we use.
        private int Vertex2DVAO;

        // Thread the window is instantiated on.
        // OpenGL is allergic to multi threading so we need to check this.
        private Thread _mainThread;
        private bool _drawingSplash;

        public override Vector2i ScreenSize => new Vector2i(_window.Width, _window.Height);

        public override void SetWindowTitle(string title)
        {
            _window.Title = title;
        }

        public override void Initialize()
        {
            _initWindow();
            ReloadConfig();
        }

        public void ProcessInput(FrameEventArgs frameEventArgs)
        {
            _window.ProcessEvents();
        }

        public void Ready()
        {
            _drawingSplash = false;
        }

        public override void ReloadConfig()
        {
            base.ReloadConfig();

            _window.VSync = VSync ? VSyncMode.On : VSyncMode.Off;
            _window.WindowState = WindowMode == WindowMode.Fullscreen ? WindowState.Fullscreen : WindowState.Normal;
        }

        private void _initWindow()
        {
            _window = new GameWindow(
                800,
                600,
                GraphicsMode.Default,
                "Space Station 14",
                GameWindowFlags.Default,
                DisplayDevice.Default,
                4, 5,
#if DEBUG
                GraphicsContextFlags.Debug | GraphicsContextFlags.ForwardCompatible
#else
                GraphicsContextFlags.ForwardCompatible
#endif
            )
            {
                Visible = true
            };

            _mainThread = Thread.CurrentThread;

            _window.KeyDown += (sender, eventArgs) =>
            {
                if (eventArgs.IsRepeat)
                {
                    return;
                }

                _gameController.GameController.KeyDown((KeyEventArgs) eventArgs);
            };

            _window.KeyUp += (sender, eventArgs) => { _gameController.GameController.KeyUp((KeyEventArgs) eventArgs); };
            _window.Closed += (sender, eventArgs) => { _gameController.GameController.Shutdown("Window closed"); };
            _window.Resize += (sender, eventArgs) =>
            {
                GL.Viewport(0, 0, _window.Width, _window.Height);
            };

            _initOpenGL();
        }

        private void _initOpenGL()
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Logger.DebugS("ogl", "OpenGL Vendor: {0}", GL.GetString(StringName.Vendor));
            Logger.DebugS("ogl", "OpenGL Version: {0}", GL.GetString(StringName.Version));

#if DEBUG
            _hijackCallback();
#endif

            Vertex2DProgram = _compileProgram(
                new ResourcePath("/Shaders/Internal/sprite.vert"),
                new ResourcePath("/Shaders/Internal/sprite.frag"));

            Vertex2DUniformModel = GL.GetUniformLocation(Vertex2DProgram, "modelMatrix");
            Vertex2DUniformView = GL.GetUniformLocation(Vertex2DProgram, "viewMatrix");
            Vertex2DUniformProjection = GL.GetUniformLocation(Vertex2DProgram, "projectionMatrix");
            Vertex2DUniformModUV = GL.GetUniformLocation(Vertex2DProgram, "modifyUV");
            Vertex2DUniformModulate = GL.GetUniformLocation(Vertex2DProgram, "modulate");

            // Vertex2D VAO.
            GL.CreateVertexArrays(1, out Vertex2DVAO);
            GL.VertexArrayAttribFormat(Vertex2DVAO, 0, 2, VertexAttribType.Float, false, 0);
            GL.EnableVertexArrayAttrib(Vertex2DVAO, 0);
            GL.VertexArrayAttribFormat(Vertex2DVAO, 1, 2, VertexAttribType.Float, false, sizeof(float) * 2);
            GL.EnableVertexArrayAttrib(Vertex2DVAO, 1);
            GL.VertexArrayAttribBinding(Vertex2DVAO, 0, 0);
            GL.VertexArrayAttribBinding(Vertex2DVAO, 1, 0);

            GL.CreateBuffers(1, out AnotherVBO);
            GL.NamedBufferStorage(AnotherVBO, sizeof(float) * 65536 * 4, IntPtr.Zero,
                BufferStorageFlags.DynamicStorageBit);

            var quadVertices = new[]
            {
                new Vertex2D(1, 0, 1, 1),
                new Vertex2D(0, 0, 0, 1),
                new Vertex2D(1, 1, 1, 0),
                new Vertex2D(0, 1, 0, 0),
            };

            GL.CreateBuffers(1, out QuadVBO);
            GL.ObjectLabel(ObjectLabelIdentifier.Buffer, QuadVBO, 7, "QuadVBO");
            GL.NamedBufferStorage(QuadVBO, sizeof(float) * 16, quadVertices, BufferStorageFlags.None);

            GL.CreateBuffers(1, out AnotherEBO);
            GL.NamedBufferStorage(AnotherEBO, sizeof(ushort) * 65536 * 4 / 6, IntPtr.Zero,
                BufferStorageFlags.DynamicStorageBit);

            _drawingSplash = true;
            Render(null);
        }

        private void _displaySplash(IRenderHandle handle)
        {
            var texture = _resourceCache.GetResource<TextureResource>("/Textures/Logo/logo.png").Texture;

            var drawHandle = handle.CreateHandleScreen();
            drawHandle.DrawTexture(texture, (ScreenSize - texture.Size) / 2);
        }

        private static void _hijackCallback()
        {
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

        private int _compileProgram(ResourcePath vertex, ResourcePath fragment)
        {
            string vertexSource;
            string fragmentSource;

            using (var vertexReader = new StreamReader(_resourceCache.ContentFileRead(vertex), Encoding.UTF8))
            {
                vertexSource = vertexReader.ReadToEnd();
            }

            using (var fragmentReader = new StreamReader(_resourceCache.ContentFileRead(fragment), Encoding.UTF8))
            {
                fragmentSource = fragmentReader.ReadToEnd();
            }

            var vertexShader = GL.CreateShader(ShaderType.VertexShader);

            try
            {
                GL.ShaderSource(vertexShader, vertexSource);
                GL.CompileShader(vertexShader);

                GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out var status);
                if (status == 0)
                {
                    var log = GL.GetShaderInfoLog(vertexShader);

                    throw new ShaderCompilationException($"Vertex shader {vertex} failed to compile:\n{log}");
                }

                var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

                try
                {
                    GL.ShaderSource(fragmentShader, fragmentSource);
                    GL.CompileShader(fragmentShader);

                    GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
                    if (status == 0)
                    {
                        var log = GL.GetShaderInfoLog(fragmentShader);

                        throw new ShaderCompilationException($"Fragment shader {fragment} failed to compile:\n{log}");
                    }

                    var program = GL.CreateProgram();

                    GL.AttachShader(program, vertexShader);
                    GL.AttachShader(program, fragmentShader);
                    GL.LinkProgram(program);

                    GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status);
                    if (status == 0)
                    {
                        var log = GL.GetProgramInfoLog(program);
                        GL.DeleteProgram(program);

                        throw new ShaderCompilationException($"program {vertex},{fragment} failed to link:\n{log}");
                    }

                    return program;
                }
                finally
                {
                    GL.DeleteShader(fragmentShader);
                }
            }
            finally
            {
                GL.DeleteShader(vertexShader);
            }
        }

        private delegate void DebugMessageCallbackDelegate([MarshalAs(UnmanagedType.FunctionPtr)] DebugProc proc,
            IntPtr userParam);

        private static void _debugMessageCallback(DebugSource source, DebugType type, int id, DebugSeverity severity,
            int length, IntPtr message, IntPtr userParam)
        {
            var contents = $"{source}: {type}: " + Marshal.PtrToStringAnsi(message, length);

            switch (severity)
            {
                case DebugSeverity.DontCare:
                    Logger.InfoS("ogl.debug", contents);
                    break;
                case DebugSeverity.DebugSeverityNotification:
                    Logger.InfoS("ogl.debug", contents);
                    break;
                case DebugSeverity.DebugSeverityHigh:
                    Logger.ErrorS("ogl.debug", contents);
                    break;
                case DebugSeverity.DebugSeverityMedium:
                    Logger.ErrorS("ogl.debug", contents);
                    break;
                case DebugSeverity.DebugSeverityLow:
                    Logger.WarningS("ogl.debug", contents);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        }

        private static readonly DebugProc _debugMessageCallbackInstance = new DebugProc(_debugMessageCallback);

        public void Dispose()
        {
            _window.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct Vertex2D
        {
            public readonly Vector2 Position;
            public readonly Vector2 TextureCoordinates;

            public Vertex2D(Vector2 position, Vector2 textureCoordinates)
            {
                Position = position;
                TextureCoordinates = textureCoordinates;
            }

            public Vertex2D(float x, float y, float u, float v)
                : this(new Vector2(x, y), new Vector2(u, v))
            {
            }

            public Vertex2D(Vector2 position, float u, float v)
                : this(position, new Vector2(u, v))
            {
            }
        }
    }

    [Serializable]
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
