using System;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using SS14.Client.Input;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Utility;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using Matrix3 = SS14.Shared.Maths.Matrix3;

namespace SS14.Client.Graphics
{
    internal class DisplayManagerOpenGL : DisplayManager, IDisplayManagerOpenGL, IDisposable
    {
        private OpenTK.GameWindow _window;

        private int VAO;
        private int VBO;
        private int ShaderProgram;

        private static readonly float[] Vertices = {
            -0.5f, -0.5f,
            0.5f, -0.5f,
            0.0f,  0.5f
        };

        private DateTime _startTime;

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

        public void Render(FrameEventArgs args)
        {
            if (GameController.Mode != GameController.DisplayMode.OpenGL)
            {
                return;
            }

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(ShaderProgram);

            var uniform = GL.GetUniformLocation(ShaderProgram, "ourColor");
            var uniformTransform = GL.GetUniformLocation(ShaderProgram, "transform");
            var time = DateTime.Now - _startTime;
            var seconds = (float)time.TotalSeconds % 5;
            var color = Color.FromHsv(new Shared.Maths.Vector4(seconds / 5f, 1, 0.75f, 0));
            GL.Uniform4(uniform, new OpenTK.Vector4(color.R, color.G, color.B, color.A));
            var matrix = Matrix3.Identity;
            matrix.Rotate(360f / 5 * seconds);
            var cmatrix = matrix.ConvertOpenTK();
            GL.UniformMatrix3(uniformTransform, false, ref cmatrix);

            GL.BindVertexArray(VAO);
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
                1280,
                720,
                GraphicsMode.Default,
                "Space Station 14",
                GameWindowFlags.Default,
                DisplayDevice.Default,
                4, 5,
                GraphicsContextFlags.Debug | GraphicsContextFlags.ForwardCompatible)
            {
                Visible = true
            };

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
        }

        private void _initOpenGL()
        {
            _startTime = DateTime.Now;
            var vendor = GL.GetString(StringName.Vendor);
            var version = GL.GetString(StringName.Version);
            Logger.DebugS("ogl", "OpenGL Vendor: {0}", vendor);
            Logger.DebugS("ogl", "OpenGL Version: {0}", version);
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            _hijackCallback();

            GL.ClearColor(0, 0, 0, 1);

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            VBO = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.NamedBufferStorage(VBO, sizeof(float) * Vertices.Length, Vertices, BufferStorageFlags.None);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), IntPtr.Zero);
            GL.EnableVertexAttribArray(0);

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

        private static void _hijackCallback()
        {
            GCHandle.Alloc(_debugMessageCallbackInstance);
            // See https://github.com/opentk/opentk/issues/880
            var type = typeof(GL);
            var entryPoints = (IntPtr[]) type.GetField("EntryPoints", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
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

        public void Dispose()
        {
            _window.Dispose();
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
}
";
    }
}
