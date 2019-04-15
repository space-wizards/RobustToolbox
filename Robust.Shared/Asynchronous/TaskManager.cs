using System.Threading;

namespace Robust.Shared.Asynchronous
{
    internal class TaskManager : ITaskManager
    {
        private SS14SynchronizationContext _mainThreadContext;

        public void Initialize()
        {
            _mainThreadContext = new SS14SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(_mainThreadContext);
        }

        public void ProcessPendingTasks()
        {
            _mainThreadContext.ProcessPendingTasks();
        }
    }

    public interface ITaskManager
    {
        void Initialize();
        void ProcessPendingTasks();
    }
}
