using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using SS14.Client.Input;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.ResourceManagement;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Graphics.Clyde
{
    /// <summary>
    ///     Responsible for most things rendering on OpenGL mode.
    /// </summary>
    internal sealed partial class Clyde : DisplayManager, IDisplayManagerOpenGL, IDisposable
    {
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager;

        private GameWindow _window;

        private Buffer BatchVBO;
        private Buffer BatchEBO;
        private OGLHandle BatchVAO;

        // VBO to draw a single quad.
        private Buffer QuadVBO;
        private OGLHandle QuadVAO;

        private OGLHandle Vertex2DProgram;

        // Locations of a few uniforms in the above program.
        private int Vertex2DUniformModel;
        private int Vertex2DUniformView;
        private int Vertex2DUniformModUV;
        private int Vertex2DUniformProjection;

        private int Vertex2DUniformModulate;

        // Thread the window is instantiated on.
        // OpenGL is allergic to multi threading so we need to check this.
        private Thread _mainThread;
        private bool _drawingSplash;

        public override Vector2i ScreenSize => new Vector2i(_window.Width, _window.Height);
        private readonly HashSet<string> OpenGLExtensions = new HashSet<string>();

        public bool HasKHRDebug => HasExtension("GL_KHR_debug");

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

        public Vector2 MouseScreenPosition
        {
            get
            {
                var state = OpenTK.Input.Mouse.GetState();
                return new Vector2(state.X, state.Y);
            }
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
                4, 1,
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

            _window.KeyUp += (sender, eventArgs) =>
            {
                _gameController.GameController.KeyUp((KeyEventArgs) eventArgs);
            };
            _window.Closed += (sender, eventArgs) =>
            {
                _gameController.GameController.Shutdown("Window closed");
            };
            _window.Resize += (sender, eventArgs) =>
            {
                GL.Viewport(0, 0, _window.Width, _window.Height);
            };
            _window.MouseDown += (sender, eventArgs) =>
            {
                _gameController.GameController.KeyDown((KeyEventArgs)eventArgs);
                _gameController.GameController.MouseDown((MouseButtonEventArgs) eventArgs);
            };
            _window.MouseUp += (sender, eventArgs) =>
            {
                _gameController.GameController.KeyUp((KeyEventArgs)eventArgs);
                _gameController.GameController.MouseUp((MouseButtonEventArgs) eventArgs);
            };
            _window.MouseMove += (sender, eventArgs) =>
            {
                _gameController.GameController.MouseMove((MouseMoveEventArgs)eventArgs);
            };
            _window.MouseWheel += (sender, eventArgs) =>
            {
                _gameController.GameController.MouseWheel((MouseWheelEventArgs)eventArgs);
            };

            _initOpenGL();
        }

        private void _initOpenGL()
        {
            _loadExtensions();

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Logger.DebugS("ogl", "OpenGL Vendor: {0}", GL.GetString(StringName.Vendor));
            Logger.DebugS("ogl", "OpenGL Version: {0}", GL.GetString(StringName.Version));

#if DEBUG
            _hijackDebugCallback();
#endif

            Vertex2DProgram = _compileProgram(
                new ResourcePath("/Shaders/Internal/sprite.vert"),
                new ResourcePath("/Shaders/Internal/sprite.frag"));

            _objectLabelMaybe(ObjectLabelIdentifier.Program, Vertex2DProgram, "Vertex2DProgram");

            Vertex2DUniformModel = GL.GetUniformLocation(Vertex2DProgram.Handle, "modelMatrix");
            Vertex2DUniformView = GL.GetUniformLocation(Vertex2DProgram.Handle, "viewMatrix");
            Vertex2DUniformProjection = GL.GetUniformLocation(Vertex2DProgram.Handle, "projectionMatrix");
            Vertex2DUniformModUV = GL.GetUniformLocation(Vertex2DProgram.Handle, "modifyUV");
            Vertex2DUniformModulate = GL.GetUniformLocation(Vertex2DProgram.Handle, "modulate");

            var quadVertices = new[]
            {
                new Vertex2D(1, 0, 1, 1),
                new Vertex2D(0, 0, 0, 1),
                new Vertex2D(1, 1, 1, 0),
                new Vertex2D(0, 1, 0, 0),
            };

            QuadVBO = new Buffer<Vertex2D>(this, BufferTarget.ArrayBuffer, BufferUsageHint.StaticDraw, quadVertices, "QuadVBO");

            QuadVAO = new OGLHandle(GL.GenVertexArray());
            GL.BindVertexArray(QuadVAO.Handle);
            _objectLabelMaybe(ObjectLabelIdentifier.VertexArray, QuadVAO, "QuadVAO");
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            BatchVBO = new Buffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw, Vertex2D.SizeOf * BatchVertexData.Length, "BatchVBO");

            BatchVAO = new OGLHandle(GL.GenVertexArray());
            GL.BindVertexArray(BatchVAO.Handle);
            _objectLabelMaybe(ObjectLabelIdentifier.VertexArray, BatchVAO, "BatchVAO");
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            BatchEBO = new Buffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw, sizeof(ushort) * BatchIndexData.Length, "BatchEBO");

            _drawingSplash = true;
            Render(null);
        }

        private void _displaySplash(IRenderHandle handle)
        {
            var texture = _resourceCache.GetResource<TextureResource>("/Textures/Logo/logo.png").Texture;

            var drawHandle = handle.CreateHandleScreen();
            drawHandle.DrawTexture(texture, (ScreenSize - texture.Size) / 2);
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

        private OGLHandle _compileProgram(ResourcePath vertex, ResourcePath fragment)
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
            _objectLabelMaybe(ObjectLabelIdentifier.Shader, new OGLHandle(vertexShader), vertex.ToString());

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
                _objectLabelMaybe(ObjectLabelIdentifier.Shader, new OGLHandle(fragmentShader), fragment.ToString());

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

                    return new OGLHandle(program);
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
        private void _objectLabelMaybe(ObjectLabelIdentifier identifier, OGLHandle name, string label)
        {
            if (!HasKHRDebug)
            {
                return;
            }

            GL.ObjectLabel(identifier, name.Handle, label.Length, label);
        }

        public void Dispose()
        {
            _window.Dispose();
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
            public readonly int Handle;

            public OGLHandle(int handle)
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
                return Handle;
            }

            public override string ToString()
            {
                return $"{nameof(Handle)}: {Handle}";
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
