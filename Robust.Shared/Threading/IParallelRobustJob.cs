using Schedulers;

namespace Robust.Shared.Threading;

/// <summary>
/// Runs the job with the specified batch size per thread; Execute is still called per index.
/// </summary>
public interface IParallelRobustJob : IJobParallelFor
{
    /*
     * Overwrite these as we don't want them exposed ot callers in case of library updates.
     */

    int IJobParallelFor.ThreadCount => 0;

    void IJobParallelFor.Finish()
    {

    }
}

