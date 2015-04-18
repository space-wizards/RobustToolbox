using SFML.Window;
using SS14.Client.Graphics.Event;
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
        void MouseWheelMove(MouseWheelEventArgs e);
        void MouseEntered(EventArgs e);
        void MouseLeft(EventArgs e);
        void FormResize();
    }
}