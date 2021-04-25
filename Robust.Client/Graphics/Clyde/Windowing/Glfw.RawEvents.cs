using System;
using System.Runtime.InteropServices;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.Input;

namespace Robust.Client.Graphics.Clyde
{
    partial class Clyde
    {
        private unsafe partial class GlfwWindowingImpl
        {
            // Keep delegates around to prevent GC issues.
            private GLFWCallbacks.ErrorCallback? _errorCallback;
            private GLFWCallbacks.MonitorCallback? _monitorCallback;
            private GLFWCallbacks.CharCallback? _charCallback;
            private GLFWCallbacks.CursorPosCallback? _cursorPosCallback;
            private GLFWCallbacks.KeyCallback? _keyCallback;
            private GLFWCallbacks.MouseButtonCallback? _mouseButtonCallback;
            private GLFWCallbacks.ScrollCallback? _scrollCallback;
            private GLFWCallbacks.WindowCloseCallback? _windowCloseCallback;
            private GLFWCallbacks.WindowSizeCallback? _windowSizeCallback;
            private GLFWCallbacks.WindowContentScaleCallback? _windowContentScaleCallback;
            private GLFWCallbacks.WindowIconifyCallback? _windowIconifyCallback;
            private GLFWCallbacks.WindowFocusCallback? _windowFocusCallback;

            private void StoreCallbacks()
            {
                _errorCallback = OnGlfwError;
                _monitorCallback = OnGlfwMonitor;
                _charCallback = OnGlfwChar;
                _cursorPosCallback = OnGlfwCursorPos;
                _keyCallback = OnGlfwKey;
                _mouseButtonCallback = OnGlfwMouseButton;
                _scrollCallback = OnGlfwScroll;
                _windowCloseCallback = OnGlfwWindowClose;
                _windowSizeCallback = OnGlfwWindowSize;
                _windowContentScaleCallback = OnGlfwWindowContentScale;
                _windowIconifyCallback = OnGlfwWindowIconify;
                _windowFocusCallback = OnGlfwWindowFocus;
            }

            private void SetupGlobalCallbacks()
            {
                GLFW.SetMonitorCallback(_monitorCallback);
            }

            private void OnGlfwMonitor(Monitor* monitor, ConnectedState state)
            {
                ref var ev = ref _glfwEventQueue.AllocAdd();
                ev.Type = GlfwEventType.Monitor;

                ev.Monitor.Monitor = monitor;
                ev.Monitor.State = state;
            }

            private void ProcessGlfwEventMonitor(in GlfwEventMonitor ev)
            {
                if (ev.State == ConnectedState.Connected)
                {
                    SetupMonitor(ev.Monitor);
                }
                else
                {
                    DestroyMonitor(ev.Monitor);
                }
            }

            private void OnGlfwChar(Window* window, uint codepoint)
            {
                ref var ev = ref _glfwEventQueue.AllocAdd();
                ev.Type = GlfwEventType.Char;

                ev.Char.CodePoint = codepoint;
            }

            private void OnGlfwCursorPos(Window* window, double x, double y)
            {
                ref var ev = ref _glfwEventQueue.AllocAdd();
                ev.Type = GlfwEventType.CursorPos;

                ev.CursorPos.Window = window;
                ev.CursorPos.XPos = x;
                ev.CursorPos.YPos = y;
            }


            private void OnGlfwKey(Window* window, Keys key, int scanCode, InputAction action, KeyModifiers mods)
            {
                ref var ev = ref _glfwEventQueue.AllocAdd();
                ev.Type = GlfwEventType.Key;

                ev.Key.Window = window;
                ev.Key.Key = key;
                ev.Key.ScanCode = scanCode;
                ev.Key.Action = action;
                ev.Key.Mods = mods;
            }


            private void OnGlfwMouseButton(Window* window, MouseButton button, InputAction action, KeyModifiers mods)
            {
                ref var ev = ref _glfwEventQueue.AllocAdd();
                ev.Type = GlfwEventType.MouseButton;

                ev.MouseButton.Window = window;
                ev.MouseButton.Button = button;
                ev.MouseButton.Action = action;
                ev.MouseButton.Mods = mods;
            }

            private void OnGlfwScroll(Window* window, double offsetX, double offsetY)
            {
                ref var ev = ref _glfwEventQueue.AllocAdd();
                ev.Type = GlfwEventType.Scroll;

                ev.Scroll.Window = window;
                ev.Scroll.XOffset = offsetX;
                ev.Scroll.YOffset = offsetY;
            }


            private void OnGlfwWindowClose(Window* window)
            {
                ref var ev = ref _glfwEventQueue.AllocAdd();
                ev.Type = GlfwEventType.WindowClose;

                ev.WindowClose.Window = window;
            }

            private void OnGlfwWindowSize(Window* window, int width, int height)
            {
                ref var ev = ref _glfwEventQueue.AllocAdd();
                ev.Type = GlfwEventType.WindowSize;

                ev.WindowSize.Window = window;
                ev.WindowSize.Width = width;
                ev.WindowSize.Height = height;
            }


            private void OnGlfwWindowContentScale(Window* window, float xScale, float yScale)
            {
                ref var ev = ref _glfwEventQueue.AllocAdd();
                ev.Type = GlfwEventType.WindowContentScale;

                ev.WindowContentScale.Window = window;
                ev.WindowContentScale.XScale = xScale;
                ev.WindowContentScale.YScale = yScale;
            }


            private void OnGlfwWindowIconify(Window* window, bool iconified)
            {
                ref var ev = ref _glfwEventQueue.AllocAdd();
                ev.Type = GlfwEventType.WindowIconified;

                ev.WindowIconify.Window = window;
                ev.WindowIconify.Iconified = iconified;
            }


            private void OnGlfwWindowFocus(Window* window, bool focused)
            {
                ref var ev = ref _glfwEventQueue.AllocAdd();
                ev.Type = GlfwEventType.WindowFocus;

                ev.WindowFocus.Window = window;
                ev.WindowFocus.Focused = focused;
            }

            private enum GlfwEventType
            {
                Invalid = 0,
                MouseButton,
                CursorPos,
                Scroll,
                Key,
                Char,
                Monitor,
                WindowClose,
                WindowFocus,
                WindowSize,
                WindowIconified,
                WindowContentScale,
            }

#pragma warning disable 649
            // ReSharper disable NotAccessedField.Local
            [StructLayout(LayoutKind.Explicit)]
            private struct GlfwEvent
            {
                [FieldOffset(0)] public GlfwEventType Type;

                [FieldOffset(0)] public GlfwEventMouseButton MouseButton;
                [FieldOffset(0)] public GlfwEventCursorPos CursorPos;
                [FieldOffset(0)] public GlfwEventScroll Scroll;
                [FieldOffset(0)] public GlfwEventKey Key;
                [FieldOffset(0)] public GlfwEventChar Char;
                [FieldOffset(0)] public GlfwEventWindowClose WindowClose;
                [FieldOffset(0)] public GlfwEventWindowSize WindowSize;
                [FieldOffset(0)] public GlfwEventWindowContentScale WindowContentScale;
                [FieldOffset(0)] public GlfwEventWindowIconify WindowIconify;
                [FieldOffset(0)] public GlfwEventWindowFocus WindowFocus;
                [FieldOffset(0)] public GlfwEventMonitor Monitor;
            }

            private struct GlfwEventMouseButton
            {
                public GlfwEventType Type;

                public Window* Window;
                public MouseButton Button;
                public InputAction Action;
                public KeyModifiers Mods;
            }

            private struct GlfwEventCursorPos
            {
                public GlfwEventType Type;

                public Window* Window;
                public double XPos;
                public double YPos;
            }

            private struct GlfwEventScroll
            {
                public GlfwEventType Type;

                public Window* Window;
                public double XOffset;
                public double YOffset;
            }

            private struct GlfwEventKey
            {
                public GlfwEventType Type;

                public Window* Window;
                public Keys Key;
                public int ScanCode;
                public InputAction Action;
                public KeyModifiers Mods;
            }

            private struct GlfwEventChar
            {
                public GlfwEventType Type;

                public Window* Window;
                public uint CodePoint;
            }

            private struct GlfwEventWindowClose
            {
                public GlfwEventType Type;

                public Window* Window;
            }

            private struct GlfwEventWindowSize
            {
                public GlfwEventType Type;

                public Window* Window;
                public int Width;
                public int Height;
            }

            private struct GlfwEventWindowContentScale
            {
                public GlfwEventType Type;

                public Window* Window;
                public float XScale;
                public float YScale;
            }

            private struct GlfwEventWindowIconify
            {
                public GlfwEventType Type;

                public Window* Window;
                public bool Iconified;
            }

            private struct GlfwEventWindowFocus
            {
                public GlfwEventType Type;

                public Window* Window;
                public bool Focused;
            }

            private struct GlfwEventMonitor
            {
                public GlfwEventType Type;

                public Monitor* Monitor;
                public ConnectedState State;
            }
            // ReSharper restore NotAccessedField.Local
#pragma warning restore 649
        }
    }
}
