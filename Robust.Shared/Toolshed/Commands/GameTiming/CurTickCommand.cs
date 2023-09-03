using System;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.Toolshed.Commands.GameTiming;

[ToolshedCommand]
public sealed partial class CurTickCommand : ToolshedCommand
{
    [Dependency] private IGameTiming _gameTiming = default!;

    [CommandImplementation]
    public GameTick CurTime() => _gameTiming.CurTick;
}
