using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using SS14.Client.Input;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using Box2 = SS14.Shared.Maths.Box2;
using Matrix3 = SS14.Shared.Maths.Matrix3;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Graphics
{
    internal partial class DisplayManagerOpenGL : DisplayManager, IDisplayManagerOpenGL, IDisposable
    {
        [Dependency]
        private readonly IResourceCache _resourceCache;
        private OpenTK.GameWindow _window;

        private int SplashVBO;

        private int Vertex2DProgram;
        private int Vertex2DVAO;

        private static readonly float[] SplashVertices =
        {
            -0.5f, 0.5f, 0, 0,
            0.5f, 0.5f, 1, 0,
            0.5f, -0.5f, 1, 1,
            -0.5f, -0.5f, 0, 1,
        };

        private int VAO;
        private int VBO;
        private int ShaderProgram;
        private Thread _mainThread;

        private static readonly float[] Vertices =
        {
            -0.5f, -0.5f,
            0.5f, -0.5f,
            0.0f, 0.5f
        };

        private DateTime _startTime;

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

        public void DisplaySplash()
        {
            var texture = (OpenGLTexture)_resourceCache.GetResource<TextureResource>("/Textures/Logo/logo.png").Texture;
            var loaded = _loadedTextures[texture.OpenGLTextureId];

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.BindTextureUnit(0, loaded.OpenGLObject);
            GL.UseProgram(Vertex2DProgram);

            var uiBox = UIBox2i.FromDimensions((ScreenSize - texture.Size) / 2, texture.Size);
            var box = ScreenToOGL(uiBox);
            GL.NamedBufferSubData(SplashVBO, IntPtr.Zero, sizeof(float) * 16, new float[]
            {
                box.Left, box.Top, 0, 0,
                box.Right, box.Top, 1, 0,
                box.Right, box.Bottom, 1, 1,
                box.Left, box.Bottom, 0, 1,
            });

            GL.BindVertexArray(Vertex2DVAO);
            GL.BindVertexBuffer(0, SplashVBO, IntPtr.Zero, 4 * sizeof(float));
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            _window.SwapBuffers();
        }

        public void Render(FrameEventArgs args)
        {
            if (GameController.Mode != GameController.DisplayMode.OpenGL)
            {
                return;
            }

            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(ShaderProgram);

            var uniform = GL.GetUniformLocation(ShaderProgram, "ourColor");
            var uniformTransform = GL.GetUniformLocation(ShaderProgram, "transform");
            var time = DateTime.Now - _startTime;
            var seconds = (float) time.TotalSeconds % 5;
            var color = Color.FromHsv(new Shared.Maths.Vector4(seconds / 5f, 1, 0.75f, 1));
            GL.Uniform4(uniform, new OpenTK.Vector4(color.R, color.G, color.B, color.A));
            var matrix = Matrix3.Identity;
            matrix.Rotate(360f / 5 * seconds);
            var cmatrix = matrix.ConvertOpenTK();
            GL.UniformMatrix3(uniformTransform, false, ref cmatrix);

            GL.BindVertexArray(VAO);
            GL.BindVertexBuffer(0, VBO, IntPtr.Zero, 2 * sizeof(float));
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            _window.SwapBuffers();
        }

        public void Input(FrameEventArgs eventArgs)
        {
            if (GameController.Mode == GameController.DisplayMode.OpenGL)
            {
                _window.ProcessEvents();
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
                4, 5,
                GraphicsContextFlags.Debug | GraphicsContextFlags.ForwardCompatible)
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
            _window.Resize += (sender, eventArgs) => { GL.Viewport(0, 0, _window.Width, _window.Height); };

            _initOpenGL();

            _window.SwapBuffers();
        }

        private void _initOpenGL()
        {
            GL.Enable(EnableCap.Blend);

            _startTime = DateTime.Now;

            Logger.DebugS("ogl", "OpenGL Vendor: {0}", GL.GetString(StringName.Vendor));
            Logger.DebugS("ogl", "OpenGL Version: {0}", GL.GetString(StringName.Version));

            _hijackCallback();

            // Gen VBO.
            GL.CreateBuffers(1, out VBO);
            GL.NamedBufferStorage(VBO, sizeof(float) * Vertices.Length, Vertices, BufferStorageFlags.None);

            // Gen VAO.
            GL.CreateVertexArrays(1, out VAO);
            GL.VertexArrayAttribFormat(VAO, 0, 2, VertexAttribType.Float, false, 0);
            GL.EnableVertexArrayAttrib(VAO, 0);
            GL.VertexArrayAttribBinding(VAO, 0, 0);


            {
                var vert = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vert, VertexShader);
                GL.CompileShader(vert);

                GL.GetShader(vert, ShaderParameter.CompileStatus, out var success);
                if (success == 0)
                {
                    var why = GL.GetShaderInfoLog(vert);
                    throw new InvalidOperationException();
                }

                var frag = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(frag, FragmentShader);
                GL.CompileShader(frag);

                GL.GetShader(frag, ShaderParameter.CompileStatus, out success);
                if (success == 0)
                {
                    var why = GL.GetShaderInfoLog(vert);
                    throw new InvalidOperationException();
                }

                ShaderProgram = GL.CreateProgram();
                GL.AttachShader(ShaderProgram, vert);
                GL.AttachShader(ShaderProgram, frag);
                GL.LinkProgram(ShaderProgram);

                GL.GetProgram(ShaderProgram, GetProgramParameterName.LinkStatus, out success);
                if (success == 0)
                {
                    throw new InvalidOperationException();
                }

                GL.DeleteShader(vert);
                GL.DeleteShader(frag);
            }

            {
                var vert = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vert, VertexShaderTexture);
                GL.CompileShader(vert);

                GL.GetShader(vert, ShaderParameter.CompileStatus, out var success);
                if (success == 0)
                {
                    var why = GL.GetShaderInfoLog(vert);
                    throw new InvalidOperationException();
                }

                var frag = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(frag, FragmentShaderTexture);
                GL.CompileShader(frag);

                GL.GetShader(frag, ShaderParameter.CompileStatus, out success);
                if (success == 0)
                {
                    var why = GL.GetShaderInfoLog(vert);
                    throw new InvalidOperationException();
                }

                Vertex2DProgram = GL.CreateProgram();
                GL.AttachShader(Vertex2DProgram, vert);
                GL.AttachShader(Vertex2DProgram, frag);
                GL.LinkProgram(Vertex2DProgram);

                GL.GetProgram(Vertex2DProgram, GetProgramParameterName.LinkStatus, out success);
                if (success == 0)
                {
                    throw new InvalidOperationException();
                }

                GL.DeleteShader(vert);
                GL.DeleteShader(frag);
            }

            // Vertex2D VAO.
            GL.CreateVertexArrays(1, out Vertex2DVAO);
            GL.VertexArrayAttribFormat(Vertex2DVAO, 0, 2, VertexAttribType.Float, false, 0);
            GL.EnableVertexArrayAttrib(Vertex2DVAO, 0);
            GL.VertexArrayAttribFormat(Vertex2DVAO, 1, 2, VertexAttribType.Float, false, sizeof(float) * 2);
            GL.EnableVertexArrayAttrib(Vertex2DVAO, 1);
            GL.VertexArrayAttribBinding(Vertex2DVAO, 0, 0);
            GL.VertexArrayAttribBinding(Vertex2DVAO, 1, 0);

            GL.CreateBuffers(1, out SplashVBO);
            GL.NamedBufferStorage(SplashVBO, sizeof(float) * SplashVertices.Length, IntPtr.Zero, BufferStorageFlags.DynamicStorageBit);
        }

        private static void _hijackCallback()
        {
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            GCHandle.Alloc(_debugMessageCallbackInstance);
            // See https://github.com/opentk/opentk/issues/880
            var type = typeof(GL);
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

        /// <summary>
        ///     Screen coords in pixels (0,0 top left, Y+ down) to OGL coordinates (0,0 center Y+ up, -1->1)
        /// </summary>
        [Pure]
        private Vector2 ScreenToOGL(Vector2i coordinates)
        {
            var c = coordinates - (Vector2)ScreenSize / 2;
            c *= new Vector2i(1, -1);
            return c * 2 / ScreenSize;
        }

        [Pure]
        private Box2 ScreenToOGL(UIBox2i coordinates)
        {
            var bl = ScreenToOGL(coordinates.BottomLeft);
            return Box2.FromDimensions(bl, coordinates.Size * 2 / (Vector2)ScreenSize);
        }

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

            public Vertex2D(float x, float y, float u, float w)
                : this(new Vector2(x, y), new Vector2(u, w))
            {
            }
        }

        private const string VertexShader = @"
#version 450 core
layout (location = 0) in vec2 aPos;

uniform mat3 transform;

void main()
{
    gl_Position = vec4(transform * vec3(aPos.xy, 0), 1.0);
}";

        private const string FragmentShader = @"
#version 450 core
out vec4 FragColor;

uniform vec4 ourColor;

void main()
{
    FragColor = ourColor;
}";

        private const string VertexShaderTexture = @"
#version 450 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 tPos;

out vec2 TexCoord;

void main()
{
    gl_Position = vec4(aPos, 0.0, 1.0);
    TexCoord = tPos;
}";

        private const string FragmentShaderTexture = @"
#version 450 core
out vec4 FragColor;

in vec2 TexCoord;

uniform sampler2D ourTexture;

void main()
{
    FragColor = texture(ourTexture, TexCoord);
}";
    }
}
