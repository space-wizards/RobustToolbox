using System.Threading;

namespace Robust.Shared.Threading;

/// <summary>
/// Implement for code that needs to be runnable on a threadpool.
/// </summary>
public interface IRobustJob
{
    void Execute();
}
