using System;
using OpenTK;
using SFML.Window;

namespace SS14.Client.Interfaces.State
{
    public interface IStateManager
    {
        Client.State.State CurrentState { get; }

        void RequestStateChange<T>()
            where T : Client.State.State;

        void Update(FrameEventArgs e);
        void Render(FrameEventArgs e);
        void KeyDown(KeyEventArgs e);
        void KeyUp(KeyEventArgs e);
        void MouseUp(MouseButtonEventArgs e);
        void MouseDown(MouseButtonEventArgs e);
        void MouseMove(MouseMoveEventArgs e);
        void MouseWheelMove(MouseWheelScrollEventArgs e);
        void MouseEntered(EventArgs e);
        void MouseLeft(EventArgs e);
        void FormResize();
        void TextEntered(TextEventArgs e);
    }
}
