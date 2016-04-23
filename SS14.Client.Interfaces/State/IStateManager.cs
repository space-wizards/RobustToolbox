

using SFML.Window;
using SS14.Client.Graphics.Event;
using System;

namespace SS14.Client.Interfaces.State
{
    public interface IStateManager
    {
        IState CurrentState { get; }
        void RequestStateChange<T>() where T : IState;
        void Update(FrameEventArgs args);
        void KeyDown(KeyEventArgs e);
        void KeyUp(KeyEventArgs e);
        void MouseUp(MouseButtonEventArgs e);
        void MouseDown(MouseButtonEventArgs e);
        void MouseMove(MouseMoveEventArgs e);
        void MouseWheelMove(MouseWheelEventArgs e);
        void MouseEntered(EventArgs e);
        void MouseLeft(EventArgs e);
        void FormResize();
        void TextEntered(TextEventArgs e);
    }
}