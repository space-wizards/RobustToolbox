using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using System;

namespace SS14.Client.Interfaces.State
{
    public interface IState
    {
        void Startup();
        void Shutdown();
        void Update(FrameEventArgs e);
        void Render(FrameEventArgs e);
        void KeyDown(KeyEventArgs e);
        void KeyUp(KeyEventArgs e);
        void MousePressed(MouseButtonEventArgs e);
        void MouseUp(MouseButtonEventArgs e);
        void MouseDown(MouseButtonEventArgs e);
        void MouseMoved(MouseMoveEventArgs e);
        void MouseMove(MouseMoveEventArgs e);
        void MouseWheelMove(MouseWheelScrollEventArgs e);
        void MouseEntered(EventArgs e);
        void MouseLeft(EventArgs e);
        void FormResize();
        void TextEntered(TextEventArgs e);
    }
}
