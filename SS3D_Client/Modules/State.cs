using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace SS13.Modules
{
    /************************************************************************/
    /* base class for program states                                        */
    /************************************************************************/
    public abstract class State
    {
        public Program Program { get; set; }

        public abstract bool Startup(Program program);

        public abstract void Shutdown();

        public abstract void Update(FrameEventArgs e);
        public abstract void GorgonRender( FrameEventArgs e );

        public abstract void KeyDown(KeyboardInputEventArgs e);
        public abstract void KeyUp(KeyboardInputEventArgs e);
        public abstract void MouseUp(MouseInputEventArgs e);
        public abstract void MouseDown(MouseInputEventArgs e);
        public abstract void MouseMove(MouseInputEventArgs e);
        public abstract void MouseWheelMove(MouseInputEventArgs e);
        public abstract void FormResize();
    }

}
