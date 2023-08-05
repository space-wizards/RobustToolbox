using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Toolshed;

namespace Robust.Shared.GameObjects;

[ToolshedCommand]
internal sealed class EntitySystemUpdateOrderCommand : ToolshedCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    [CommandImplementation("tick")]
    public IEnumerable<Type> Tick()
    {
        var mgr = (EntitySystemManager)_entitySystemManager;

        return mgr.TickUpdateOrder;
    }

    [CommandImplementation("frame")]
    public IEnumerable<Type> Frame()
    {
        var mgr = (EntitySystemManager)_entitySystemManager;

        return mgr.FrameUpdateOrder;
    }
}
