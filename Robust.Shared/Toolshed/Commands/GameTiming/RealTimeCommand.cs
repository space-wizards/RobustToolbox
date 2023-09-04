using System;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.Toolshed.Commands.GameTiming;

[ToolshedCommand]
[InjectDependencies]
public sealed partial class RealTimeCommand : ToolshedCommand
{
    [Dependency] private IGameTiming _gameTiming = default!;

    [CommandImplementation]
    public TimeSpan CurTime() => _gameTiming.RealTime;
}
