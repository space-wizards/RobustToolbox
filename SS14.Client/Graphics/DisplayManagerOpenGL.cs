using System;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using SS14.Client.Input;
using SS14.Client.Interfaces.Graphics;
using SS14.Shared.Log;

namespace SS14.Client.Graphics
{
    internal class DisplayManagerOpenGL : DisplayManager, IDisplayManagerOpenGL, IDisposable
    {
        private GameWindow _window;

        private int VBO;

        private static readonly float[] Vertices = {
            -0.5f, -0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            0.0f,  0.5f, 0.0f
        };

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
            GCHandle.Alloc(_debugMessageCallbackInstance);

            System.Console.WriteLine(GL.GetString(StringName.Version) + GL.GetString(StringName.Vendor) + GL.GetString(StringName.ShadingLanguageVersion));

            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            _hijackCallback();

            GL.ClearColor(0, 0, 0, 1);

            VBO = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.NamedBufferStorage(VBO, sizeof(float) * Vertices.Length, Vertices, BufferStorageFlags.None);
        }

        private static void _hijackCallback()
        {
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
layout (location = 0) in vec3 aPos;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0);
}";

        private const string FragmentShader = @"
#version 450 core
out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
}
";
    }
}
