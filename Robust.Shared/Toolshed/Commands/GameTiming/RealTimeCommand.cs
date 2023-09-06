using System;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.Toolshed.Commands.GameTiming;

[ToolshedCommand]
public sealed class RealTimeCommand : ToolshedCommand
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [CommandImplementation]
    public TimeSpan CurTime() => _gameTiming.RealTime;
}
