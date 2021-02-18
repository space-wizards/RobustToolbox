using System;
using System.Threading;
using Robust.Shared.Exceptions;
using Robust.Shared.IoC;

namespace Robust.Shared.Asynchronous
{
    internal sealed class TaskManager : ITaskManager
    {
        private RobustSynchronizationContext _mainThreadContext = default!;

        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;

        public void Initialize()
        {
            _mainThreadContext = new RobustSynchronizationContext(_runtimeLog);
            SynchronizationContext.SetSynchronizationContext(_mainThreadContext);
        }

        public void ProcessPendingTasks()
        {
            _mainThreadContext.ProcessPendingTasks();
        }

        public void RunOnMainThread(Action callback)
        {
            _mainThreadContext.Post(_runCallback, callback);
        }

        private static readonly SendOrPostCallback _runCallback = o =>
        {
            ((Action?)o)?.Invoke();
        };
    }

    public interface ITaskManager
    {
        void Initialize();
        void ProcessPendingTasks();

        /// <summary>
        ///     Run a delegate on the main thread sometime later.
        ///     Thread safe.
        /// </summary>
        /// <remarks>
        ///     Useful if you want to run a callback from a separate thread.
        /// </remarks>
        /// <param name="callback">The callback that will be invoked on the main thread.</param>
        void RunOnMainThread(Action callback);
    }
}
