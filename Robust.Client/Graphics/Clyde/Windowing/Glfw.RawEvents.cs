using System.Threading.Tasks;
using OpenToolkit.GraphicsLibraryFramework;

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
            private GLFWCallbacks.WindowPosCallback? _windowPosCallback;
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
                _windowPosCallback = OnGlfwWindowPos;
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
                if (state == ConnectedState.Connected)
                    WinThreadSetupMonitor(monitor);
                else
                    WinThreadDestroyMonitor(monitor);
            }

            private void OnGlfwChar(Window* window, uint codepoint)
            {
                SendEvent(new EventChar((nint) window, codepoint));
            }

            private void OnGlfwCursorPos(Window* window, double x, double y)
            {
                SendEvent(new EventCursorPos((nint) window, x, y));
            }

            private void OnGlfwKey(Window* window, Keys key, int scanCode, InputAction action, KeyModifiers mods)
            {
                SendEvent(new EventKey((nint) window, key, scanCode, action, mods));
            }

            private void OnGlfwMouseButton(Window* window, MouseButton button, InputAction action, KeyModifiers mods)
            {
                SendEvent(new EventMouseButton((nint) window, button, action, mods));
            }

            private void OnGlfwScroll(Window* window, double offsetX, double offsetY)
            {
                SendEvent(new EventScroll((nint) window, offsetX, offsetY));
            }

            private void OnGlfwWindowClose(Window* window)
            {
                SendEvent(new EventWindowClose((nint) window));
            }

            private void OnGlfwWindowSize(Window* window, int width, int height)
            {
                GLFW.GetFramebufferSize(window, out var fbW, out var fbH);
                SendEvent(new EventWindowSize((nint) window, width, height, fbW, fbH));
            }

            private void OnGlfwWindowPos(Window* window, int x, int y)
            {
                SendEvent(new EventWindowPos((nint) window, x, y));
            }

            private void OnGlfwWindowContentScale(Window* window, float xScale, float yScale)
            {
                SendEvent(new EventWindowContentScale((nint) window, xScale, yScale));
            }

            private void OnGlfwWindowIconify(Window* window, bool iconified)
            {
                SendEvent(new EventWindowIconify((nint) window, iconified));
            }

            private void OnGlfwWindowFocus(Window* window, bool focused)
            {
                SendEvent(new EventWindowFocus((nint) window, focused));
            }

            // NOTE: events do not correspond 1:1 to GLFW events
            // This is because they need to pack all the data required
            // for the game-thread event handling.

            private abstract record EventBase;

            private record EventMouseButton(
                nint Window,
                MouseButton Button,
                InputAction Action,
                KeyModifiers Mods
            ) : EventBase;

            private record EventCursorPos(
                nint Window,
                double XPos,
                double YPos
            ) : EventBase;

            private record EventScroll(
                nint Window,
                double XOffset,
                double YOffset
            ) : EventBase;

            private record EventKey(
                nint Window,
                Keys Key,
                int ScanCode,
                InputAction Action,
                KeyModifiers Mods
            ) : EventBase;

            private record EventChar
            (
                nint Window,
                uint CodePoint
            ) : EventBase;

            private record EventWindowClose
            (
                nint Window
            ) : EventBase;

            private record EventWindowCreate(
                GlfwWindowCreateResult Result,
                TaskCompletionSource<GlfwWindowCreateResult> Tcs
            ) : EventBase;

            private record EventWindowSize
            (
                nint Window,
                int Width,
                int Height,
                int FramebufferWidth,
                int FramebufferHeight
            ) : EventBase;

            private record EventWindowPos
            (
                nint Window,
                int X,
                int Y
            ) : EventBase;

            private record EventWindowContentScale
            (
                nint Window,
                float XScale,
                float YScale
            ) : EventBase;

            private record EventWindowIconify
            (
                nint Window,
                bool Iconified
            ) : EventBase;

            private record EventWindowFocus
            (
                nint Window,
                bool Focused
            ) : EventBase;

            private record EventMonitorSetup
            (
                int Id,
                string Name,
                VideoMode Mode
            ) : EventBase;

            private record EventMonitorDestroy
            (
                int Id
            ) : EventBase;
        }
    }
}
