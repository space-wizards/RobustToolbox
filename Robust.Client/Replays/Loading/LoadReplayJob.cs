using Robust.Shared.ContentPack;
using System.Threading.Tasks;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Utility;

namespace Robust.Client.Replays.Loading;

/// <summary>
/// Simple job for loading some replay file. Note that tick updates need to be blocked
/// (<see cref="IGameController.TickUpdateOverride"/>) in order to avoid unexpected errors.
/// </summary>
[Virtual]
public class LoadReplayJob : Job<bool>
{
    private readonly IWritableDirProvider _dir;
    private readonly ResPath _path;
    private readonly IReplayLoadManager _loadMan;

    public LoadReplayJob(
        float maxTime,
        IWritableDirProvider dir,
        ResPath path,
        IReplayLoadManager loadMan)
        : base(maxTime)
    {
        _dir = dir;
        _path = path;
        _loadMan = loadMan;
    }

    protected override async Task<bool> Process()
    {
        await _loadMan.LoadAndStartReplayAsync(_dir, _path, Yield);
        return true;
    }

    protected virtual async Task Yield(float value, float maxValue, LoadingState state, bool force)
    {
        // Content inheritors can update some UI or loading indicator here

        if (force)
            await SuspendNow();
        else
            await SuspendIfOutOfTime();
    }
}
