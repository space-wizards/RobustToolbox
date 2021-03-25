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

        public virtual void FrameUpdate(FrameEventArgs e) { }
    }
}
