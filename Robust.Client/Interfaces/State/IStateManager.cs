using System;
using Robust.Client.Input;

namespace Robust.Client.Interfaces.State
{
    public interface IStateManager
    {
        Client.State.State CurrentState { get; }
        void RequestStateChange<T>() where T : Client.State.State, new();
        void Update(ProcessFrameEventArgs e);
        void FrameUpdate(RenderFrameEventArgs e);
        void MouseUp(MouseButtonEventArgs e);
        void MouseDown(MouseButtonEventArgs e);
        void MouseMove(MouseMoveEventArgs e);
        void MouseWheelMove(MouseWheelEventArgs e);
        void FormResize();
    }
}
