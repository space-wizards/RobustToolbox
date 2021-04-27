using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Input;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        private void SendKeyUp(KeyEventArgs ev)
        {
            if (_initialized)
                KeyUp?.Invoke(ev);
        }

        private void SendKeyDown(KeyEventArgs ev)
        {
            if (_initialized)
                KeyDown?.Invoke(ev);
        }

        private void SendScroll(MouseWheelEventArgs ev)
        {
            if (_initialized)
                MouseWheel?.Invoke(ev);
        }

        private void SendCloseWindow(WindowClosedEventArgs ev)
        {
            CloseWindow?.Invoke(ev);
        }

        private void SendWindowResized(WindowReg reg, Vector2i oldSize)
        {
            if (reg.IsMainWindow)
            {
                UpdateMainWindowLoadedRtSize();
                GL.Viewport(0, 0, reg.FramebufferSize.X, reg.FramebufferSize.Y);
                CheckGlError();
            }
            else
            {
                reg.RenderTexture!.Dispose();
                CreateWindowRenderTexture(reg);
            }

            var eventArgs = new WindowResizedEventArgs(
                oldSize,
                reg.FramebufferSize,
                reg.Handle);

            if (_initialized)
                OnWindowResized?.Invoke(eventArgs);
        }

        private void SendWindowContentScaleChanged()
        {
            if (_initialized)
                OnWindowScaleChanged?.Invoke();
        }

        private void SendWindowFocus(WindowFocusedEventArgs ev)
        {
            if (_initialized)
                OnWindowFocused?.Invoke(ev);
        }

        private void SendText(TextEventArgs ev)
        {
            if (_initialized)
                TextEntered?.Invoke(ev);
        }
    }
}
