using System;
using SS14.Client.Input;

namespace SS14.Client.State
{
    public abstract class State
    {
        /// <summary>
        ///     Screen is being (re)enabled.
        /// </summary>
        public abstract void Startup();

        /// <summary>
        ///     Screen is being disabled (NOT Destroyed).
        /// </summary>
        public abstract void Shutdown();

        /// <summary>
        ///     Update the contents of this screen.
        /// </summary>
        public virtual void Update(ProcessFrameEventArgs e) { }

        public virtual void FrameUpdate(RenderFrameEventArgs e) { }

        #region Events

        /// <summary>
        ///     Key was pressed.
        /// </summary>
        public virtual void KeyDown(KeyEventArgs e) { }

        /// <summary>
        ///     Key was released.
        /// </summary>
        public virtual void KeyUp(KeyEventArgs e) { }

        /// <summary>
        ///     Key was is STILL held.
        /// </summary>
        public virtual void KeyHeld(KeyEventArgs e) { }

        /// <summary>
        ///     Mouse button was pressed.
        /// </summary>
        public virtual void MousePressed(MouseButtonEventArgs e) { }

        /// <summary>
        ///     Mouse button was released.
        /// </summary>
        public virtual void MouseUp(MouseButtonEventArgs e) { }

        /// <summary>
        ///     Mouse button will be pressed.
        /// </summary>
        public virtual void MouseDown(MouseButtonEventArgs e) { }

        /// <summary>
        ///     Mouse cursor has moved.
        /// </summary>
        public virtual void MouseMoved(MouseMoveEventArgs e) { }

        /// <summary>
        ///     Mouse cursor will move.
        /// </summary>
        public virtual void MouseMove(MouseMoveEventArgs e) { }

        /// <summary>
        ///     Mouse wheel has been moved.
        /// </summary>
        public virtual void MouseWheelMove(MouseWheelEventArgs e) { }

        /// <summary>
        ///     Mouse has entered this screen.
        /// </summary>
        public virtual void MouseEntered(EventArgs e) { }

        /// <summary>
        ///     Left mouse button has been pressed.
        /// </summary>
        public virtual void MouseLeft(EventArgs e) { }

        /// <summary>
        ///     The screen has changed size, usually from resizing window. This is called automatically right after Startup.
        /// </summary>
        public virtual void FormResize() { }

        #endregion Events
    }
}
