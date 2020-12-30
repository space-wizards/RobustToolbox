using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Robust.Shared.Asynchronous
{

    /// <summary>
    /// An implementation of TaskScheduler that uses the ThreadPool scheduler
    /// </summary>
    public sealed class RobustTaskScheduler : TaskScheduler
    {

        public static readonly RobustTaskScheduler Instance = new();

        private RobustTaskScheduler()
        {
            // private
        }

        public static void Execute(object? o)
        {
            switch (o)
            {
                case IThreadPoolWorkItem w:
                    w.Execute();
                    break;
                case ValueTask v:
                    Instance.TryExecuteTask(v.AsTask());
                    break;
                case Task t:
                    Instance.TryExecuteTask(t);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Schedules a task to the ThreadPool.
        /// </summary>
        /// <param name="task">The task to schedule.</param>
        protected override void QueueTask(Task task)
        {
            if (task.IsCompleted) return;

            var options = task.CreationOptions;
            if ((options & TaskCreationOptions.LongRunning) == 0)
            {
                ThreadPool.UnsafeQueueUserWorkItem(Execute, task, false);
            }
            else
            {
                var wantsToBeFair = (task.CreationOptions & TaskCreationOptions.PreferFairness) != 0;
                new Thread(Execute)
                {
                    Priority = wantsToBeFair ? ThreadPriority.BelowNormal : ThreadPriority.AboveNormal,
                    IsBackground = true
                }.Start(task);
            }
        }

        /// <summary>
        /// This internal function will do this:
        ///   (1) If the task had previously been queued, attempt to pop it and return false if that fails.
        ///   (2) Return whether the task is executed
        ///
        /// IMPORTANT NOTE: TryExecuteTaskInline will NOT throw task exceptions itself. Any wait code path using this function needs
        /// to account for exceptions that need to be propagated, and throw themselves accordingly.
        /// </summary>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return task.IsCompleted;
        }

        protected override bool TryDequeue(Task task) => false;

        protected override IEnumerable<Task> GetScheduledTasks() => Enumerable.Empty<Task>();

    }

}
