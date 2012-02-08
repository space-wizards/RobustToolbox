using ClientInterfaces.Network;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientInterfaces.State
{
    public interface IState
    {
        void Startup();
        void Shutdown();
        void Update(FrameEventArgs e);
        void GorgonRender(FrameEventArgs e);
        void KeyDown(KeyboardInputEventArgs e);
        void KeyUp(KeyboardInputEventArgs e);
        void MouseUp(MouseInputEventArgs e);
        void MouseDown(MouseInputEventArgs e);
        void MouseMove(MouseInputEventArgs e);
        void MouseWheelMove(MouseInputEventArgs e);
        void FormResize();
    }
}
