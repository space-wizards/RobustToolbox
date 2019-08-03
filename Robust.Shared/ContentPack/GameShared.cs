using System;
using Robust.Shared.Timing;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Common entry point for Content assemblies.
    /// </summary>
    public abstract class GameShared : IDisposable
    {
        protected ModuleTestingCallbacks TestingCallbacks { get; private set; }

        public void SetTestingCallbacks(ModuleTestingCallbacks testingCallbacks)
        {
            TestingCallbacks = testingCallbacks;
        }

        public virtual void Init()
        {
        }

        public virtual void PostInit()
        {
        }

        public virtual void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        ~GameShared()
        {
            Dispose(false);
        }
    }
}
