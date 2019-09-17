using Robust.Shared.Timing;

namespace Robust.Client.State
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
        public virtual void Update(FrameEventArgs e) { }

        public virtual void FrameUpdate(FrameEventArgs e) { }

        /// <summary>
        ///     The screen has changed size, usually from resizing window. This is called automatically right after Startup.
        /// </summary>
        public virtual void FormResize() { }
    }
}
