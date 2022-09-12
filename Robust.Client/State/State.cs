using System;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Client.State
{
    public abstract class State
    {
        //[Optional] The UIScreen attached to this gamestate
        protected virtual Type? LinkedScreenType => null;

        /// <summary>
        ///     Game switching to this state
        /// </summary>

        internal void StartupInternal(IUserInterfaceManager userInterfaceManager)
        {
            if (LinkedScreenType != null)
            {
                if (!LinkedScreenType.IsAssignableTo(typeof(UIScreen))) throw new Exception("Linked Screen type is invalid");
                userInterfaceManager.LoadScreenInternal(LinkedScreenType);
            }
            Startup();
        }

        protected abstract void Startup();

        /// <summary>
        ///     Game switching away from this state
        /// </summary>

        internal void ShutdownInternal(IUserInterfaceManager userInterfaceManager)
        {
            if (LinkedScreenType != null)
            {
                userInterfaceManager.UnloadScreen();
            }
            Shutdown();
        }

        protected abstract void Shutdown();

        public virtual void FrameUpdate(FrameEventArgs e) { }
    }
}
