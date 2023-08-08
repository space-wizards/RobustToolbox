using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Common entry point for Content assemblies.
    /// </summary>
    public abstract class GameShared : IDisposable
    {
        protected internal IDependencyCollection Dependencies { get; internal set; } = default!;
        protected List<ModuleTestingCallbacks> TestingCallbacks { get; private set; } = new();

        public void SetTestingCallbacks(List<ModuleTestingCallbacks> testingCallbacks)
        {
            TestingCallbacks = testingCallbacks;
        }

        public virtual void PreInit()
        {
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

        public virtual void Shutdown()
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
    }
}
