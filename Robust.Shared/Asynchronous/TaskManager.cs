using System;
using System.Threading;
using System.Threading.Tasks;
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
            ResetSynchronizationContext();
        }

        public void ResetSynchronizationContext()
        {
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

        public void BlockWaitOnTask(Task task)
        {
            // NOTE: This code should be re-entry safe.
            while (true)
            {
                var waitTask = _mainThreadContext.WaitOnPendingTasks().AsTask();
                var idx = Task.WaitAny(task, waitTask);
                if (idx == 0)
                    return;

                _mainThreadContext.ProcessPendingTasks();
            }
        }

        private static readonly SendOrPostCallback _runCallback = o => { ((Action?)o)?.Invoke(); };
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

        /// <summary>
        /// Synchronously wait for a main-thread task to complete.
        /// This is effectively what you need to safely .Result a task on the main thread.
        /// </summary>
        /// <remarks>
        /// Use of this method is only ever recommended in rare scenarios like shutdown. For most other scenarios you should really avoid blocking the main thread and use proper async instead.
        /// </remarks>
        void BlockWaitOnTask(Task task);
    }
}
