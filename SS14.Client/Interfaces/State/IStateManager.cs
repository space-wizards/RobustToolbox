using SS14.Client.Input;
using System;

namespace SS14.Client.Interfaces.State
{
    public interface IStateManager
    {
        Client.State.State CurrentState { get; }
        void RequestStateChange<T>() where T : Client.State.State, new();
        void Update(ProcessFrameEventArgs e);
        void FrameUpdate(RenderFrameEventArgs e);
        void KeyDown(KeyEventArgs e);
        void KeyUp(KeyEventArgs e);
        void KeyHeld(KeyEventArgs e);
        void MouseUp(MouseButtonEventArgs e);
        void MouseDown(MouseButtonEventArgs e);
        void MouseMove(MouseMoveEventArgs e);
        void MouseWheelMove(MouseWheelEventArgs e);
        void FormResize();
    }
}
