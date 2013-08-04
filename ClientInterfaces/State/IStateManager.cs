using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientInterfaces.State
{
    public interface IStateManager
    {
        IState CurrentState { get; }
        void RequestStateChange<T>() where T : IState;
        void Update(FrameEventArgs args);
        void KeyDown(KeyboardInputEventArgs e);
        void KeyUp(KeyboardInputEventArgs e);
        void MouseUp(MouseInputEventArgs e);
        void MouseDown(MouseInputEventArgs e);
        void MouseMove(MouseInputEventArgs e);
        void MouseWheelMove(MouseInputEventArgs e);
    }
}