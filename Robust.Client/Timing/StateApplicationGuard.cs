using System;

namespace Robust.Client.Timing;

public readonly struct StateApplicationGuard : IDisposable
{
    private readonly IClientGameTiming _gameTiming;

    public StateApplicationGuard(IClientGameTiming gameTiming)
    {
        _gameTiming = gameTiming;
    }

    public void Dispose()
    {
        _gameTiming.EndStateApplication();
    }
}

