namespace Robust.Shared.CPUJob.JobQueues
{
    public interface IJob
    {
        JobStatus Status { get; }
        void Run();
    }
}
